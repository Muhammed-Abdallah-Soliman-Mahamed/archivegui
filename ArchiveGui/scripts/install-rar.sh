#!/usr/bin/env bash
# بينزّل أداة rar (لإنشاء ملفات RAR) من موقع RARLAB الرسمي ويثبّتها في /usr/local/bin.
# تثبيت اختياري وبطلب صريح من المستخدم: sudo archivegui-install-rar  (أو من install-tools.sh).
# مش بيتشغّل تلقائي من postinstall بتاع الحزمة (عشان قفل dpkg ومفيش تثبيت صامت).
# الاستخدام: install-rar.sh [--yes]     (--yes = من غير سؤال التأكيد)
# ملحوظة ترخيص: rar برنامج تجاري (RARLAB) بنسخة تجريبية؛ ArchiveGui مش بيوزّعه، المستخدم بينزّله من موقعهم.
# متغير اختياري: ARCHIVEGUI_RAR_SHA256 = قيمة SHA-256 المتوقعة للأرشيف (لو معاك قيمة موثوقة).
set -uo pipefail

BASE="https://www.rarlab.com"
assume_yes=0
[ "${1:-}" = "--yes" ] && assume_yes=1

if [ "$(id -u)" -ne 0 ]; then
  if command -v pkexec >/dev/null 2>&1; then exec pkexec bash "$0" "$@"
  elif command -v sudo >/dev/null 2>&1; then exec sudo bash "$0" "$@"
  fi
  echo "محتاج صلاحيات root لتثبيت rar." >&2; exit 1
fi

[ "$(uname -m)" = "x86_64" ] || { echo "تحميل rar الاحتياطي لـ x86_64 بس." >&2; exit 0; }

if command -v rar >/dev/null 2>&1; then echo "rar موجود بالفعل: $(command -v rar)"; exit 0; fi

if [ "$assume_yes" -ne 1 ] && [ -t 0 ]; then
  echo "هيتم تحميل rar (برنامج تجاري بنسخة تجريبية) من $BASE/download.htm وتثبيته في /usr/local/bin."
  read -r -p "أكمل؟ [y/N] " ans
  case "$ans" in y|Y|yes|YES) ;; *) echo "اتلغى."; exit 0 ;; esac
fi

TMP="$(mktemp -d)" || { echo "فشل إنشاء مجلد مؤقت." >&2; exit 1; }
trap 'rm -rf "$TMP"' EXIT      # تنضيف حتى لو حصل خطأ أو اتقطع السكربت

fetch() { # الرابط  [ملف-الخرج]  (بيطبع على stdout لو مفيش ملف)
  if command -v curl >/dev/null 2>&1; then
    if [ -n "${2:-}" ]; then curl -fsSL --proto '=https' --tlsv1.2 --max-time 120 -o "$2" "$1"
    else curl -fsSL --proto '=https' --tlsv1.2 --max-time 30 "$1"; fi
  elif command -v wget >/dev/null 2>&1; then
    if [ -n "${2:-}" ]; then wget -q --https-only --timeout=120 -O "$2" "$1"
    else wget -qO- --https-only --timeout=30 "$1"; fi
  else
    echo "مفيش curl ولا wget لتحميل rar." >&2; return 127
  fi
}

page="$(fetch "$BASE/download.htm")" || { echo "تعذّر فتح صفحة RARLAB (مفيش إنترنت؟)." >&2; exit 1; }
rel="$(printf '%s' "$page" | grep -o 'rar/rarlinux-x64-[0-9a-z.]*\.tar\.gz' | head -1)"
[ -n "$rel" ] || { echo "ما لقيناش رابط rar على موقع RARLAB." >&2; exit 1; }

echo "+ تحميل $BASE/$rel"
fetch "$BASE/$rel" "$TMP/rar.tar.gz" || { echo "فشل التحميل." >&2; exit 1; }

sum="$(sha256sum "$TMP/rar.tar.gz" | cut -d' ' -f1)"
echo "  SHA-256: $sum"
if [ -n "${ARCHIVEGUI_RAR_SHA256:-}" ] && [ "$sum" != "${ARCHIVEGUI_RAR_SHA256,,}" ]; then
  echo "SHA-256 مش مطابق للقيمة المتوقعة — اتلغى التثبيت." >&2; exit 1
fi

# فحص سلامة: لازم الأرشيف يحتوي rar/rar فقط تحت مجلد rar/ (من غير مسارات مطلقة أو ..)
if tar -tzf "$TMP/rar.tar.gz" | grep -Eq '(^/|\.\.)'; then echo "الأرشيف فيه مسارات مش آمنة." >&2; exit 1; fi
tar -xzf "$TMP/rar.tar.gz" -C "$TMP" rar/rar rar/unrar 2>/dev/null || tar -xzf "$TMP/rar.tar.gz" -C "$TMP" rar/rar \
  || { echo "فشل فك الأرشيف." >&2; exit 1; }
"$TMP/rar/rar" 2>&1 | head -n 3 | grep -qi 'RAR' || { echo "الملف المحمّل مش شغّال كـ rar." >&2; exit 1; }

install -m 755 "$TMP/rar/rar" /usr/local/bin/rar
if ! command -v unrar >/dev/null 2>&1 && [ -f "$TMP/rar/unrar" ]; then
  install -m 755 "$TMP/rar/unrar" /usr/local/bin/unrar
fi
echo "  ✓ rar اتثبّت في /usr/local/bin/rar"
exit 0
