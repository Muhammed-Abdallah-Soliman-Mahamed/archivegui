using Archive.Services;

var testFilePath = "/tmp/test-file.txt";
await File.WriteAllTextAsync(testFilePath, "مرحبا، ده ملف تجربة!");

var service = new SharpCompressArchiveService();

var progress = new Progress<double>(p => Console.WriteLine($"التقدم: {p:F1}%"));

Console.WriteLine("جاري إنشاء الأرشيف...");
await service.CreateAsync(new[] { testFilePath }, "/tmp/test.zip", progress);
Console.WriteLine("تم إنشاء /tmp/test.zip بنجاح!");

Console.WriteLine();
Console.WriteLine("محتويات الأرشيف:");
var entries = await service.ListEntriesAsync("/tmp/test.zip");
foreach (var entry in entries)
{
    Console.WriteLine($"  {entry.Path} - {entry.Size} bytes");
}

Console.WriteLine();
Console.WriteLine("جاري استخراج الأرشيف...");
await service.ExtractAsync("/tmp/test.zip", "/tmp/extracted", progress);
Console.WriteLine("تم الاستخراج في /tmp/extracted");