using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string apiUrl = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";

        using HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode)
        {
            string jsonData = await response.Content.ReadAsStringAsync();

            // Parse the JSON data
            var timeEntries = JsonSerializer.Deserialize<List<TimeEntry>>(jsonData);

            // Loading the JSON data into a usable list of data, and adding up the total hours worked
            var employeeWorkTimes = timeEntries
                .GroupBy(e => e.EmployeeName)
                .Select(g => new EmployeeWorkTime
                {
                    Name = g.Key,
                    TotalTimeWorked = g.Sum(e => (e.EndTimeUtc - e.StarTimeUtc).TotalHours)
                })
                .OrderByDescending(e => e.TotalTimeWorked)
                .ToList();

            // Generate the HTML table and save it to a file
            string htmlContent = GenerateHtmlTable(employeeWorkTimes);
            File.WriteAllText("EmployeeWorkTimeReport.html", htmlContent);
            Console.WriteLine("HTML report generated.");
        }
        else
        {
            Console.WriteLine("Failed to retrieve data from the API.");
        }
    }

    // Method to generate HTML table
    static string GenerateHtmlTable(List<EmployeeWorkTime> employeeWorkTimes)
    {
        var html = "<html><head><style>" +
                   "table {border-collapse: collapse; width: 100%;}" +
                   "th, td {border: 1px solid black; padding: 8px; text-align: left;}" +
                   "tr.low-work {background-color: #f8d7da;}" +
                   "</style></head><body>" +
                   "<h1>Employee Work Time Report</h1>" +
                   "<table>" +
                   "<tr><th>Name</th><th>Total Time Worked (hours)</th></tr>";

        foreach (var employee in employeeWorkTimes)
        {
            string rowClass = employee.TotalTimeWorked < 100 ? "low-work" : "";
            html += $"<tr class='{rowClass}'><td>{employee.Name}</td><td>{employee.TotalTimeWorked:F2}</td></tr>";
        }

        html += "</table></body></html>";
        return html;
    }

    // EmployeeWorkTime class
    public class EmployeeWorkTime
    {
        public string Name { get; set; }
        public double TotalTimeWorked { get; set; }
    }

    // TimeEntry class
    public class TimeEntry
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime StarTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public string EntryNotes { get; set; } = string.Empty;
        public DateTime? DeletedOn { get; set; }
    }
}
