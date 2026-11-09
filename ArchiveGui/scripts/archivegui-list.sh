#!/usr/bin/env bash
# بيطبع محتويات الأرشيف (بباسورد أو من غيره) كسطور:  المسار<TAB>الحجم<TAB>المضغوط<TAB>مجلد(0/1)
set -euo pipefail
source "$(dirname "$0")/_common.sh"

archive="${1:-}"
[ -f "$archive" ] || die "الاستخدام: $0 <archive>"
sz="$(need_7z)"

if have_password; then
  out="$(printf '%s\n' "$ARCHIVEGUI_PASSWORD" | "$sz" l -slt -bd -- "$archive")"
else
  out="$("$sz" l -slt -bd -- "$archive" < /dev/null)"
fi

# 7z -slt بيطبع بلوكات "Key = Value"؛ بنبدأ التجميع بعد سطر الفاصل "----------"
printf '%s\n' "$out" | awk -F' = ' '
  /^----------$/ { started=1; next }
  !started { next }
  $1=="Path"        { if (path!="") emit(); path=$2; size=0; packed=0; dir=0 }
  $1=="Size"        { size=$2 }
  $1=="Packed Size" { packed=$2 }
  $1=="Folder"      { dir=($2=="+")?1:0 }
  $1=="Attributes"  { if ($2 ~ /^D/) dir=1 }
  END { if (path!="") emit() }
  function emit() { printf "%s\t%s\t%s\t%s\n", path, size, packed, dir }
'
