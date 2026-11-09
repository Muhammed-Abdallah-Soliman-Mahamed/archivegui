#!/usr/bin/env bash
# الاستخدام: archivegui-extract.sh <أرشيف> <مجلد-الوجهة>
# ARCHIVEGUI_PASSWORD (اختياري) للأرشيفات المحمية بباسورد.
set -euo pipefail
source "$(dirname "$0")/_common.sh"

archive="${1:-}"; dest="${2:-}"
[ -f "$archive" ] && [ -n "$dest" ] || die "الاستخدام: $0 <archive> <dest-dir>"
mkdir -p -- "$dest"

lower="$(printf '%s' "$archive" | tr '[:upper:]' '[:lower:]')"

case "$lower" in
  *.tar|*.tar.gz|*.tgz|*.tar.bz2|*.tbz2|*.tar.xz|*.txz)
    command -v tar >/dev/null 2>&1 || die "أداة tar غير مثبتة."
    tar -xf "$archive" -C "$dest"
    ;;

  *.rar)
    if have_password; then pw="-p$ARCHIVEGUI_PASSWORD"; else pw="-p-"; fi
    if command -v unrar >/dev/null 2>&1; then
      unrar x -o+ -idq -y "$pw" -- "$archive" "$dest/" < /dev/null
    elif command -v rar >/dev/null 2>&1; then
      rar x -o+ -idq -y "$pw" -- "$archive" "$dest/" < /dev/null
    else
      sz="$(need_7z)"
      if have_password; then
        printf '%s\n' "$ARCHIVEGUI_PASSWORD" | "$sz" x -y -bd "-o$dest" -- "$archive"
      else
        "$sz" x -y -bd "-o$dest" -- "$archive" < /dev/null
      fi
    fi
    ;;

  *)
    if sz="$(find_7z)"; then
      if have_password; then
        # من غير -p عمدًا: 7z بيسأل عن الباسورد من stdin بس لما الأرشيف يحتاجه
        printf '%s\n' "$ARCHIVEGUI_PASSWORD" | "$sz" x -y -bd "-o$dest" -- "$archive"
      else
        "$sz" x -y -bd "-o$dest" -- "$archive" < /dev/null
      fi
    elif [[ "$lower" == *.zip ]] && command -v unzip >/dev/null 2>&1; then
      # احتياطي لما 7-Zip مش موجود
      if have_password; then
        unzip -o -q -P "$ARCHIVEGUI_PASSWORD" -d "$dest" "$archive" < /dev/null
      else
        unzip -o -q -d "$dest" "$archive" < /dev/null
      fi
    else
      die "أداة 7-Zip غير مثبتة. ثبّتها: sudo bash $(dirname "$0")/install-tools.sh"
    fi
    ;;
esac

echo "تم الاستخراج إلى: $dest"
