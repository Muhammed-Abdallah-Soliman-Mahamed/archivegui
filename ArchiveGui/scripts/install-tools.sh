#!/usr/bin/env bash
# بيثبّت أدوات الأرشفة الخارجية اللي ArchiveGui بيستخدمها: 7-Zip (zip/7z بباسورد) و unrar + rar (لصيغة RAR).
# بيتشغّل كـ root (من الحزمة أو عبر pkexec/sudo). مش بيفشل لو حاجة مش موجودة في المستودع.
set -uo pipefail
SCRIPT_DIR="$(dirname "$(readlink -f "$0")")"

if [ "$(id -u)" -ne 0 ]; then
  if command -v pkexec >/dev/null 2>&1; then exec pkexec bash "$0" "$@"
  elif command -v sudo >/dev/null 2>&1; then exec sudo bash "$0" "$@"
  fi
  echo "محتاج صلاحيات root لتثبيت الأدوات." >&2; exit 1
fi

try() { echo "+ $*"; "$@" || echo "  (تخطّينا: $*)"; }

if command -v pacman >/dev/null 2>&1; then
  try pacman -S --needed --noconfirm 7zip
  try pacman -S --needed --noconfirm unrar
  try pacman -S --needed --noconfirm rar
elif command -v apt-get >/dev/null 2>&1; then
  if command -v fuser >/dev/null 2>&1 && fuser /var/lib/dpkg/lock-frontend >/dev/null 2>&1; then
    echo "dpkg busy, try again later" >&2; exit 1
  fi
  apt-get update -qq || true
  # الاسم بيختلف حسب الإصدار: 7zip (الجديد) أو p7zip-full (القديم)
  apt-get install -y 7zip || try apt-get install -y p7zip-full
  try apt-get install -y unrar || try apt-get install -y unrar-free
  try apt-get install -y rar   # في non-free (Debian) / multiverse (Ubuntu)
elif command -v dnf >/dev/null 2>&1; then
  dnf install -y 7zip || try dnf install -y p7zip p7zip-plugins
  try dnf install -y unrar   # من RPM Fusion nonfree
  try dnf install -y rar     # من RPM Fusion nonfree
elif command -v zypper >/dev/null 2>&1; then
  try zypper --non-interactive install 7zip
  try zypper --non-interactive install unrar
else
  echo "مدير الحزم غير معروف. ثبّت يدويًا: 7zip, unrar, rar." >&2
fi

# لو rar لسه ناقص (مش في مستودع التوزيعة): تحميل احتياطي من موقع RARLAB الرسمي.
command -v rar >/dev/null 2>&1 || bash "$SCRIPT_DIR/install-rar.sh" || true

echo
for t in 7zz 7z 7za unrar rar; do
  if command -v "$t" >/dev/null 2>&1; then echo "  ✓ $t"; fi
done
command -v rar >/dev/null 2>&1 || echo "ملاحظة: 'rar' (لإنشاء RAR) مش متاح في كل المستودعات — لو ناقص، ثبّته من موقع RARLAB أو من مستودع التوزيعة."
exit 0
