#!/usr/bin/env bash
# الاستخدام: archivegui-create.sh <zip|7z|rar|tar|tar.gz|tar.bz2> <مسار-الأرشيف-الكامل> <مصدر1> [مصدر2 ...]
# متغيرات بيئة اختيارية:
#   ARCHIVEGUI_PASSWORD    الباسورد (فاضي = من غير تشفير)
#   ARCHIVEGUI_ENCRYPTION  none | zipcrypto | aes256   (الافتراضي none)
set -euo pipefail
source "$(dirname "$0")/_common.sh"

fmt="${1:-}"; dest="${2:-}"
[ -n "$fmt" ] && [ -n "$dest" ] && [ "$#" -ge 3 ] || die "الاستخدام: $0 <zip|7z|rar|tar|tar.gz|tar.bz2> <archive> <path...>"
shift 2

enc="${ARCHIVEGUI_ENCRYPTION:-none}"
encrypt=0
if have_password && [ "$enc" != "none" ]; then encrypt=1; fi

for root in "$@"; do [ -e "$root" ] || die "المصدر غير موجود: $root"; done

mkdir -p -- "$(dirname -- "$dest")"
rm -f -- "$dest"

case "$fmt" in
  tar|tar.gz|tar.bz2)
    [ "$encrypt" = 0 ] || die "صيغ tar مفيهاش تشفير. اختار zip أو 7z أو rar لو عايز باسورد."
    command -v tar >/dev/null 2>&1 || die "أداة tar غير مثبتة."
    case "$fmt" in tar.gz) flag=z ;; tar.bz2) flag=j ;; *) flag="" ;; esac
    args=(-c${flag}f "$dest")
    for root in "$@"; do
      name="$(basename -- "$root")"
      case "$name" in -*) name="./$name" ;; esac   # اسم بيبدأ بـ "-" ميتفهمش كخيار
      args+=(-C "$(dirname -- "$root")" "$name")
    done
    tar "${args[@]}"
    ;;

  zip|7z)
    if sz="$(find_7z)"; then
      args=(a "-t$fmt" -y -bd)
      if [ "$encrypt" = 1 ]; then
        args+=(-p)
        if [ "$fmt" = "zip" ]; then
          case "$enc" in
            aes256)    args+=(-mem=AES256) ;;
            zipcrypto) args+=(-mem=ZipCrypto) ;;
            *) die "نوع تشفير غير معروف: $enc" ;;
          esac
        else
          # 7z بيستخدم AES-256 دايمًا؛ -mhe=on بيشفّر أسماء الملفات كمان
          args+=(-mhe=on)
        fi
      fi
      for root in "$@"; do
        parent="$(dirname -- "$root")"; name="$(basename -- "$root")"
        if [ "$encrypt" = 1 ]; then
          ( cd "$parent" && printf '%s\n%s\n' "$ARCHIVEGUI_PASSWORD" "$ARCHIVEGUI_PASSWORD" | "$sz" "${args[@]}" -- "$dest" "$name" )
        else
          ( cd "$parent" && "$sz" "${args[@]}" -- "$dest" "$name" < /dev/null )
        fi
      done
    elif [ "$fmt" = "zip" ] && command -v zip >/dev/null 2>&1 && [ "$enc" != "aes256" ]; then
      # احتياطي لما 7-Zip مش موجود: Info-ZIP (بيدعم zip عادي و ZipCrypto بس)
      for root in "$@"; do
        parent="$(dirname -- "$root")"; name="$(basename -- "$root")"
        if [ "$encrypt" = 1 ]; then
          ( cd "$parent" && zip -r -q -P "$ARCHIVEGUI_PASSWORD" "$dest" "$name" )
        else
          ( cd "$parent" && zip -r -q "$dest" "$name" )
        fi
      done
    else
      die "أداة 7-Zip غير مثبتة (مطلوبة لـ $fmt بهذه الخيارات). ثبّتها: sudo bash $(dirname "$0")/install-tools.sh"
    fi
    ;;

  rar)
    rar_bin="$(command -v rar || true)"
    [ -n "$rar_bin" ] || die "أداة rar غير مثبتة (مغلقة المصدر). ثبّتها: sudo bash $(dirname "$0")/install-tools.sh"
    args=(a -r -idq -y)
    # RAR بيدعم AES-256 بس؛ -hp بيشفّر المحتوى وأسماء الملفات
    if [ "$encrypt" = 1 ]; then args+=("-hp$ARCHIVEGUI_PASSWORD"); fi
    for root in "$@"; do
      parent="$(dirname -- "$root")"; name="$(basename -- "$root")"
      ( cd "$parent" && "$rar_bin" "${args[@]}" -- "$dest" "$name" < /dev/null )
    done
    ;;

  *) die "صيغة غير مدعومة: $fmt" ;;
esac

echo "تم إنشاء: $dest"
