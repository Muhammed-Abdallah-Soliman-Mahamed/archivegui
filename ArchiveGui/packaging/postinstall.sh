#!/bin/sh

# تحديث قاعدة بيانات تطبيقات سطح المكتب
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database -q /usr/share/applications 2>/dev/null || true
fi

# تنبيه المستخدم لو أداة rar غير متاحة
if ! command -v rar >/dev/null 2>&1; then
    echo "ArchiveGui: لتثبيت الأدوات الإضافية شغّل: sudo archivegui-install-tools"
fi

exit 0
