using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System;
using System.Collections.Generic;
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
            
            //Parse the JSON data

            var timeEntries = JsonSerializer.Deserialize<List<TimeEntry>>(jsonData);

            if (timeEntries != null && timeEntries.Any())
            {
                var employeeWorkTimes = timeEntries
                    .GroupBy(e => e.EmployeeName)
                    .Select(g => new
                    {
                        Name = g.Key,
                        TotalTimeWorked = g.Sum(e => (e.EndTimeUtc - e.StarTimeUtc).TotalHours)
                    })
                    .ToList();

                double totalTimeWorked = employeeWorkTimes.Sum(e => e.TotalTimeWorked);

                var plotModel = new PlotModel { Title = "Employee Work Time" };

                var pieSeries = new PieSeries();

                // Adding employee slices to the chart 
                foreach (var employee in employeeWorkTimes)
                {
                    double percentage = (employee.TotalTimeWorked / totalTimeWorked) * 100;
                    pieSeries.Slices.Add(new PieSlice(employee.Name, percentage));
                }

                plotModel.Series.Add(pieSeries);

                var plotView = new PlotView
                {
                    Model = plotModel,
                    Size = new System.Drawing.Size(1200, 800)
                };

                using (var stream = File.Create("EmployeeWorkTimePieChart.png"))
                {
                    var pngExporter = new OxyPlot.WindowsForms.PngExporter { Width = 1200, Height = 800 };
                    pngExporter.Export(plotModel, stream);
                }

                Console.WriteLine("Pie chart saved as EmployeeWorkTimePieChart.png");
            }
            else
            {
                Console.WriteLine("No time entries available.");
            }
        }
        else
        {
            Console.WriteLine("Failed to retrieve data from the API.");
        }
    }

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
