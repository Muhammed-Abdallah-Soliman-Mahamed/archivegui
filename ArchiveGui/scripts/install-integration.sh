#!/usr/bin/env bash
# بيركّب "Extract Here" و "Compress to…" في مدير الملفات بتاعك (للمستخدم الحالي بس، من غير root).
# بيشتغل مع: Dolphin (KDE) و Nautilus (GNOME). Thunar بيتضاف يدوي (التعليمات تحت).
# الاستخدام:  bash install-integration.sh            ← تركيب
#             bash install-integration.sh --remove   ← إزالة
# لو الحزمة (.deb/.rpm/.pkg.tar.zst) متركّبة، القوائم بتتركّب تلقائي ومش محتاج السكربت ده.
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
kio_dir="${XDG_DATA_HOME:-$HOME/.local/share}/kio/servicemenus"
naut_dir="${XDG_DATA_HOME:-$HOME/.local/share}/nautilus/scripts"

if [ "${1:-}" = "--remove" ]; then
  rm -f "$kio_dir"/archivegui-extract.desktop "$kio_dir"/archivegui-compress.desktop
  rm -f "$naut_dir"/"Extract Here (ArchiveGui)" "$naut_dir"/"Compress to zip (ArchiveGui)" \
        "$naut_dir"/"Compress to 7z (ArchiveGui)" "$naut_dir"/"Compress to tar.gz (ArchiveGui)" \
        "$naut_dir"/"Compress to rar (ArchiveGui)" "$naut_dir"/"Compress to zip with password (ArchiveGui)"
  echo "اتشالت قوائم ArchiveGui."
  exit 0
fi

[ -f "$here/archivegui-extract-here.sh" ] || { echo "مش لاقي سكربتات ArchiveGui جنب $0" >&2; exit 1; }

# ───────────── Dolphin (KDE) ─────────────
mkdir -p "$kio_dir"
for f in archivegui-extract.desktop archivegui-compress.desktop; do
  src="$here/../packaging/servicemenus/$f"
  [ -f "$src" ] || src="/usr/share/kio/servicemenus/$f"
  if [ -f "$src" ]; then
    # بنبدّل مسار السكربتات الافتراضي بالمسار الفعلي (مفيد وقت التطوير)
    sed "s#/usr/lib/archivegui/scripts#$here#g" "$src" > "$kio_dir/$f"
    chmod +x "$kio_dir/$f"     # Plasma بيتجاهل service menus المحلية لو مش executable
  fi
done
echo "Dolphin: اتركّب في $kio_dir  (اقفل Dolphin وافتحه تاني لو مظهرش)"

# ───────────── Nautilus (GNOME Files) ─────────────
if command -v nautilus >/dev/null 2>&1; then
  mkdir -p "$naut_dir"
  mk() { # الاسم  السكربت  [وسائط ثابتة...]
    local name="$1" script="$2"; shift 2
    cat > "$naut_dir/$name" <<EOS
#!/bin/bash
files=()
while IFS= read -r f; do [ -n "\$f" ] && files+=("\$f"); done <<< "\$NAUTILUS_SCRIPT_SELECTED_FILE_PATHS"
exec bash "$here/$script" $* "\${files[@]}"
EOS
    chmod +x "$naut_dir/$name"
  }
  mk "Extract Here (ArchiveGui)"                    archivegui-extract-here.sh
  mk "Compress to zip (ArchiveGui)"                 archivegui-compress-context.sh zip
  mk "Compress to 7z (ArchiveGui)"                  archivegui-compress-context.sh 7z
  mk "Compress to tar.gz (ArchiveGui)"              archivegui-compress-context.sh tar.gz
  mk "Compress to rar (ArchiveGui)"                 archivegui-compress-context.sh rar
  mk "Compress to zip with password (ArchiveGui)"   archivegui-compress-context.sh --encrypt zip
  echo "Nautilus: اتركّب في $naut_dir (تحت Scripts في الـ right-click)"
fi

cat <<EOT

Thunar (XFCE): من Edit ← Configure custom actions أضف أمرين:
  Extract Here   :  bash "$here/archivegui-extract-here.sh" %F
  Compress to zip:  bash "$here/archivegui-compress-context.sh" zip %F
EOT
