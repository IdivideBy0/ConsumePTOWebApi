using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;


namespace ConsumeWebApi
{


    class Program
    {
        static void Main()
        {
            
            mjm_ptoEntities Context = new mjm_ptoEntities();

            var employeeids = (Context.employees
                    .Join(Context.position_history, e => e.external_id, h => h.Position_ID, (e, h) => new {e, h})
                    .Where(@t =>
                        @t.e.external_id != null && @t.e.adjusted_company_seniority != null &&
                        @t.e.external_id.Contains("62M") && @t.e.employee_type == "False" && @t.e.work_status != "T")
                    .OrderBy(@t => @t.e.external_id)
                    .Select(@t => @t.e.external_id))
                         .Distinct().ToArray();

           
            string[] urlArray = new string[employeeids.Count()];

            // Sample GET --> https://localhost:44397/api/ptoCalc?positionId=62M999999&year=2018&type=regular

            Random rnd = new Random();

            Console.WriteLine(DateTime.Now + System.Environment.NewLine + "Building Random Queries - Please Stand By:" + System.Environment.NewLine);

            for (int i = 0; i < employeeids.Count(); i++)
            {

                bool flip = (rnd.Next(1, 10) % 2) == 0;

                int[] years;
                using (var db = new mjm_ptoEntities())
                {
                    years = db.employees.Where(x => employeeids.Contains(x.external_id)).ToList()
                        .Select(x => new DateTime(
                            DateTime.Parse(x.adjusted_company_seniority.ToString()).Year,
                            DateTime.Parse(x.adjusted_company_seniority.ToString()).Month,
                            DateTime.Parse(x.adjusted_company_seniority.ToString()).Day).Year).Distinct().ToArray();

                }

                int rndYear = rnd.Next(years.Min(), years.Max());

                StringBuilder sb = new StringBuilder();

                sb.Append("https://localhost:44397/api/ptoCalc?");
                sb.Append("positionId=");
                sb.Append(employeeids[i]);
                sb.Append("&year=");
                sb.Append(rndYear.ToString());
                sb.Append("&type=");
                if (flip)
                    sb.Append("regular");
                else
                    sb.Append("probation");

                urlArray[i] = sb.ToString();

                Console.WriteLine("Query #" + (i+1) + " of " + employeeids.Count() + " " + urlArray[i] + Environment.NewLine);

                GetCalcResponse(urlArray[i]);
            }


                while (true)
                {
                    //GetSynchronously(accrualUrls);
                    GetSynchronously(urlArray);
                }

        }

        private static void GetSynchronously(string[] urlStrings)
        {
            Console.WriteLine(DateTime.Now + System.Environment.NewLine);

            for (int x = 0; x < urlStrings.Length; x++)
            {

                HttpWebRequest request = WebRequest.CreateHttp(urlStrings[x]);
                request.Method = "GET";


                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {

                    using (Stream responseStream = response.GetResponseStream())
                    {

                        using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {

                            string responseJSON = myStreamReader.ReadToEnd();


                            IEnumerable<string> query =
                                    from url in urlStrings
                                    select url.Substring(url.IndexOf("positionId") + 11, 9);


                            if (responseJSON != "[]")
                            { 

                            responseJSON += System.Environment.NewLine +
                                            System.Environment.NewLine + x.ToString() + System.Environment.NewLine;                           

                                if (responseJSON.Contains(query.ElementAt(x)))
                                    Console.WriteLine(responseJSON);
                                else
                                    Console.WriteLine("Response did not match the intended id.");

                            }   
                            else
                            {
                                Console.WriteLine(System.Environment.NewLine +
                                            System.Environment.NewLine + x.ToString() + " Accrual Unavailable with this Id " + query.ElementAt(x) + System.Environment.NewLine);
                            }

                        }
                    }

                }

            }

            Console.WriteLine(DateTime.Now + System.Environment.NewLine);
        }

        private static void GetCalcResponse(string urlString)
        {
            HttpWebRequest request = WebRequest.CreateHttp(urlString);
            request.Method = "GET";


            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {

                using (Stream responseStream = response.GetResponseStream())
                {

                    using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                    {

                        string responseJSON = myStreamReader.ReadToEnd();


                        //IEnumerable<string> query =
                        string posid =   urlString.Substring(urlString.IndexOf("positionId") + 11, 9);


                        if (responseJSON != "[]")
                        {

                            responseJSON += System.Environment.NewLine +
                                            System.Environment.NewLine;

                            if (responseJSON.Contains(posid))
                                Console.WriteLine(responseJSON);
                            else
                                Console.WriteLine("Response did not match the intended id.");

                        }
                        else
                        {
                            Console.WriteLine(System.Environment.NewLine +
                                              System.Environment.NewLine + " Accrual Unavailable with this Id " + posid + System.Environment.NewLine);
                        }

                    }
                }

            }


        }



    }

}

