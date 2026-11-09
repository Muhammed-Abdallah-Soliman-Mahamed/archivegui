#!/bin/sh
# ملحوظة: مفيش مدير حزم ولا شبكة هنا (dpkg ماسك القفل). الأدوات الأساسية بتيجي من depends/recommends.
set -e

SCRIPTS_DIR="/usr/lib/archivegui/scripts"

if [ -d "$SCRIPTS_DIR" ]; then
    chmod 0755 "$SCRIPTS_DIR"/*.sh 2>/dev/null || true
fi

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database -q /usr/share/applications 2>/dev/null || true
fi

if ! command -v rar >/dev/null 2>&1; then
    echo "ArchiveGui: لإنشاء ملفات RAR شغّل: sudo archivegui-install-tools"
fi

exit 0
