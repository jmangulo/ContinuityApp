using Microsoft.Win32;
using System.ComponentModel;
using System.IO;

namespace Connect112
{
    public class ExportViewModel
    {
        public static void ExportToCSV<T>(string testName, IEnumerable<T> data)
        {
            var properties = typeof(T).GetProperties();
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Save Results",
                FileName = $"{testName.Trim()}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(string.Join(",", properties.Where(prop => !prop.Name.Contains("index", StringComparison.CurrentCultureIgnoreCase)).Select(p =>
                    {
                        var displayNameAttribute = p.GetCustomAttributes(typeof(DisplayNameAttribute), true)
                                                    .Cast<DisplayNameAttribute>()
                                                    .FirstOrDefault();
                        return displayNameAttribute?.DisplayName ?? p.Name;
                    })));

                    foreach (var item in data)
                    {
                        var values = properties.
                            Where(prop => !prop.Name.
                            Contains("index", StringComparison.CurrentCultureIgnoreCase)).
                            Select(p => p.GetValue(item, null)?.ToString()?.
                            Replace(",", " ") ?? string.Empty);
                        writer.WriteLine(string.Join(",", values));
                    }
                }
            }
        }
    }
}
