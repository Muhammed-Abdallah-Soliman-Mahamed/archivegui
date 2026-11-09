#!/usr/bin/env bash
# "Extract Here" لقوائم الـ right-click: بيفك كل أرشيف جنبه في مجلد باسمه.
# الاستخدام: archivegui-extract-here.sh <أرشيف1> [أرشيف2 ...]
# لو الأرشيف محمي بباسورد بيفتح نافذة تطلبه (kdialog أو zenity).
set -uo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
source "$here/_common.sh"

[ "$#" -ge 1 ] || die "الاستخدام: $0 <archive...>"

status=0
for archive in "$@"; do
  name="$(basename -- "$archive")"
  dest="$(unique_path "$(dirname -- "$archive")/$(strip_archive_ext "$name")")"
  pw="${ARCHIVEGUI_PASSWORD:-}"
  attempts=0

  while :; do
    out="$(ARCHIVEGUI_PASSWORD="$pw" bash "$here/archivegui-extract.sh" "$archive" "$dest" 2>&1)"; rc=$?

    if [ "$rc" -eq 0 ]; then
      notify "ArchiveGui" "تم استخراج $name"
      break
    fi

    if [ "$rc" -eq 255 ] || printf '%s' "$out" | grep -qiE 'password|encrypted'; then
      if [ "$attempts" -lt 3 ]; then
        attempts=$((attempts + 1))
        if pw="$(ask_password "الباسورد لـ $name")" && [ -n "$pw" ]; then continue; fi
        out="اتلغى إدخال الباسورد."
      else
        out="الباسورد غلط."
      fi
    fi

    notify "ArchiveGui" "فشل استخراج $name: $(printf '%s' "$out" | tail -n 3)"
    rmdir "$dest" 2>/dev/null || true
    status=1
    break
  done
done
exit "$status"
