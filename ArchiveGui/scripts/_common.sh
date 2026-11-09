#!/usr/bin/env bash
# دوال مشتركة بين سكربتات ArchiveGui. بتتحمّل بـ: source "$(dirname "$0")/_common.sh"

die() { echo "ArchiveGui: $*" >&2; exit 2; }

# بيدور على 7-Zip بأي اسم من أسمائه الشائعة (7zz = النسخة الرسمية، 7z/7za = p7zip)
find_7z() {
  local c
  for c in 7zz 7z 7za; do
    if command -v "$c" >/dev/null 2>&1; then command -v "$c"; return 0; fi
  done
  return 1
}

need_7z() {
  find_7z || die "أداة 7-Zip غير مثبتة. شغّل install-tools.sh أو ثبّت الحزمة (7zip أو p7zip-full)."
}

# الباسورد بييجي من متغير بيئة (ARCHIVEGUI_PASSWORD) مش من سطر الأوامر،
# وبنمرره لـ 7z عبر stdin عشان ميظهرش في قائمة العمليات (ps).
have_password() { [ -n "${ARCHIVEGUI_PASSWORD:-}" ]; }

# ───────────── مساعدات قوائم مديري الملفات (Extract Here / Compress) ─────────────

# إشعار للمستخدم: kdialog (KDE) أو zenity/notify-send (GNOME وغيره)، وإلا stderr بس.
notify() {
  local title="$1" msg="$2"
  if command -v kdialog >/dev/null 2>&1; then
    kdialog --title "$title" --passivepopup "$msg" 6 >/dev/null 2>&1 || true
  elif command -v notify-send >/dev/null 2>&1; then
    notify-send "$title" "$msg" >/dev/null 2>&1 || true
  fi
  echo "$title: $msg" >&2
}

# نافذة باسورد: بترجّع الباسورد على stdout، وكود خروج غير صفري لو اتلغت أو مفيش أداة نوافذ.
ask_password() {
  local prompt="$1"
  if command -v kdialog >/dev/null 2>&1; then
    kdialog --title "ArchiveGui" --password "$prompt"
  elif command -v zenity >/dev/null 2>&1; then
    zenity --password --title="ArchiveGui: $prompt"
  else
    return 1
  fi
}

# مسار مش موجود: لو "x" موجود بيرجّع "x (1)" وهكذا. الاستخدام: unique_path <مسار> [امتداد-مع-نقطة]
unique_path() {
  local base="$1" ext="${2:-}" p="$1${2:-}" n=1
  while [ -e "$p" ]; do p="$base ($n)$ext"; n=$((n + 1)); done
  printf '%s\n' "$p"
}

# اسم الملف من غير امتداد الأرشفة (بيفهم .tar.gz و .tar.bz2)
strip_archive_ext() {
  local name="$1" lower
  lower="$(printf '%s' "$name" | tr '[:upper:]' '[:lower:]')"
  case "$lower" in
    *.tar.gz|*.tar.bz2|*.tar.xz) printf '%s\n' "${name%.*.*}" ;;
    *.*)                         printf '%s\n' "${name%.*}" ;;
    *)                           printf '%s\n' "$name" ;;
  esac
}
