using Newtonsoft.Json;
using RareCrewAssignment.Classfiles;
using ScottPlot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Generator;

namespace RareCrewAssignment.Controllers
{
    public class HomeController : Controller
    {
        private static readonly HttpClient _http = new HttpClient();
        // GET: Home
        public async Task<ActionResult> Index()
        {
            string Url = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
            var json = await _http.GetStringAsync(Url);
            DataTable dt= JsonConvert.DeserializeObject<DataTable>(json);
            dt.Columns.Add("WorkingHours",typeof(double));
            foreach (DataRow dr in dt.Rows) {
                DateTime starTime = Convert.ToDateTime(dr["StarTimeUtc"]);
                DateTime endTime = Convert.ToDateTime(dr["EndTimeUtc"]);
                dr["WorkingHours"] = ((endTime- starTime).TotalMinutes )/60;
                
            }

            List<Employee> employees = dt.AsEnumerable()
            .GroupBy(e => e["EmployeeName"].ToString())
            .Select(g => new Employee
            {
                EmployeeName = g.Key,
                TotalWorkingHours = Math.Round(
                    g.Sum(row => row.Field<double>("WorkingHours")), 2)
            })
            .OrderByDescending(e=> e.TotalWorkingHours).ToList();
            ViewBag.EmployeeData=employees;
            Session["EmployeeData"] = employees;

            return View();
        }

        public ActionResult GeneratePNG() {


            List<Employee> emp = Session["EmployeeData"] as List<Employee>;

            double total = emp.Sum(e=> e.TotalWorkingHours);
            var palette = new ScottPlot.Palettes.Category10();

            var slices = new List<PieSlice>();
            int i = 0;
            foreach (var item in emp)
            {
                double percent = item.TotalWorkingHours / total * 100;
                string name = string.IsNullOrWhiteSpace(item.EmployeeName) ? "Unknown" : item.EmployeeName;
                slices.Add(new PieSlice
                {
                    Value = item.TotalWorkingHours,
                    FillColor = GetSliceColor(i++),
                    Label = $"{name} ({percent:0.0}%)"
                });
            }

            var plot = new Plot();
            plot.Add.Pie(slices);
            plot.Axes.Frameless();
            plot.HideGrid();
            plot.ShowLegend();
            plot.Title("Working Hours of Employee");

            //plot.SavePng("piechart.png", 700, 500);string folder = Server.MapPath("~/Content/Charts");
            string folder = Server.MapPath("~/Documents/Charts");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string filename = "piechart_" + DateTime.Now.ToString("dd_MM_yyyy_mm_hh") + ".png";
            string path = Path.Combine(folder,  filename);
            plot.SavePng(path, 700, 500);
             path = Path.Combine(Server.MapPath("~/Documents/Charts"), filename);

            if (!System.IO.File.Exists(path))
                return HttpNotFound();

            return File(path, "image/png", filename);
            

        }

        static ScottPlot.Color GetSliceColor(int index)
        {
            // 0.618 (golden ratio) scatters hues so neighboring slices look different
            float hue = (index * 0.618034f) % 1f;
            return ScottPlot.Color.FromHSL(hue, 0.6f, 0.55f);
        }
    }
}