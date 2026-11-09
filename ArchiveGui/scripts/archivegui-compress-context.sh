#!/usr/bin/env bash
# "Compress to…" لقوائم الـ right-click: بيضغط الملفات/المجلدات المحددة جنب أول واحد فيهم.
# الاستخدام: archivegui-compress-context.sh [--encrypt] <zip|7z|rar|tar|tar.gz|tar.bz2> <مسار1> [مسار2 ...]
#   --encrypt  بيطلب باسورد في نافذة (مرتين للتأكيد) ويشفّر بـ AES-256
set -uo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
source "$here/_common.sh"

encrypt=0
if [ "${1:-}" = "--encrypt" ]; then encrypt=1; shift; fi

fmt="${1:-}"; shift || true
[ -n "$fmt" ] && [ "$#" -ge 1 ] || die "الاستخدام: $0 [--encrypt] <zip|7z|rar|tar.gz|...> <path...>"

first="${1%/}"
parent="$(dirname -- "$first")"
if [ "$#" -eq 1 ]; then
  base="$(basename -- "$first")"
  [ -d "$first" ] || base="${base%.*}"      # ملف: نشيل امتداده. مجلد: نسيب اسمه.
else
  base="$(basename -- "$parent")"            # أكتر من عنصر: اسم المجلد اللي جواه
  [ -n "$base" ] && [ "$base" != "/" ] || base="Archive"
fi
[ -n "$base" ] || base="Archive"

dest="$(unique_path "$parent/$base" ".$fmt")"

if [ "$encrypt" = 1 ]; then
  case "$fmt" in tar|tar.gz|tar.bz2) notify "ArchiveGui" "صيغة $fmt مفيهاش تشفير."; exit 1 ;; esac
  pw1="$(ask_password "اكتب الباسورد")" || { notify "ArchiveGui" "اتلغى الضغط."; exit 1; }
  pw2="$(ask_password "أكّد الباسورد")" || { notify "ArchiveGui" "اتلغى الضغط."; exit 1; }
  if [ -z "$pw1" ] || [ "$pw1" != "$pw2" ]; then notify "ArchiveGui" "الباسورد فاضي أو مش متطابق."; exit 1; fi
  export ARCHIVEGUI_PASSWORD="$pw1" ARCHIVEGUI_ENCRYPTION="aes256"
fi

clean=()
for p in "$@"; do clean+=("${p%/}"); done

if out="$(bash "$here/archivegui-create.sh" "$fmt" "$dest" "${clean[@]}" 2>&1)"; then
  notify "ArchiveGui" "تم إنشاء $(basename -- "$dest")"
else
  notify "ArchiveGui" "فشل الضغط: $(printf '%s' "$out" | tail -n 3)"
  exit 1
fi
