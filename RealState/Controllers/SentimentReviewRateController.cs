using FireSharp.Interfaces;
using FireSharp.Response;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RealState.Models;
using RealState.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using System.Drawing.Printing;
using System.Net.Http;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

namespace RealState.Controllers
{
    public class SentimentReviewRateController : Controller
    {
        //firebase
        private static string Apikey = "AIzaSyBmuTMtbd9-jCEH55r7eAvI1YC0NC-wLXY";
        private static string Bucket = "realestate-e6dce.appspot.com";
        private static string AuthEmail = "asramhmd130@gmail.com";
        private static string Authpassword = "Esraa#777";
        IFirebaseConfig config = new FireSharp.Config.FirebaseConfig
        {
            AuthSecret = "iadmfN20Fsw6gtWkpeA2ZICLuWDrRIqTDNGLUQLb",
            BasePath = "https://realestate-e6dce-default-rtdb.firebaseio.com/"
        };

        public IFirebaseConfig Config { get => config; set => config = value; }
        IFirebaseClient client;

        MySession session = MySession.Instance;

        public ActionResult My_User_Review(int? page)
        {

            //Get All Users2Ids
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response = client.Get("Users2Ids");
            dynamic data = JsonConvert.DeserializeObject<dynamic>(response.Body);
            var all_ReviewRate = new List<ReviewRate>();
            if (data != null)
            {
                foreach (var item in data)
                {
                    if (item != null) // add another null check here
                    {
                        all_ReviewRate.Add(JsonConvert.DeserializeObject<ReviewRate>(((JProperty)item).Value.ToString()));
                    }
                }
            }

            // get all Users2Ids that contain Buyer ID 
            var BuyerReviewRate = new List<ReviewRate>();
            foreach (var r in all_ReviewRate)
            {
                if (r.BuyerId == session.user_id)
                {
                    BuyerReviewRate.Add(r);
                }
            }

            //Get All Users
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response2 = client.Get("Users");
            dynamic data2 = JsonConvert.DeserializeObject<dynamic>(response2.Body);
            var all_Users = new List<SignUpModel>();
            foreach (var item in data2)
            {
                all_Users.Add(JsonConvert.DeserializeObject<SignUpModel>(((JProperty)item).Value.ToString()));
            }

            //Get Users that treated with Buyer
            var UsersWithBuyer = new List<SignUpModel>();
            foreach (var r in BuyerReviewRate)
            {
                foreach (var u in all_Users)
                {
                    if (r.SellerId == u.SignUp_ID)
                    {
                        UsersWithBuyer.Add(u);
                    }
                }       
            }    
            if (UsersWithBuyer.Count() == 0)
            {
                return RedirectToAction("ReviewNotFound");
            }
            else
            {
                var Users = UsersWithBuyer.ToPagedList(page ?? 1, UsersWithBuyer.Count());
                return View(Users);
            }
        }
        
        public async Task<ActionResult> ReviewSentiment(string Seller_id)
        {
            //get Review from view 
            string review = Request.Form["review"];
            string Sentiment_Category = "";
            //using Sentiment Model to know if text positive or negative
            // Check if input text is not empty
            if (!string.IsNullOrEmpty(review))
            {
                //string jsonResponse="";
                string apiUrl = "http://localhost:5000/";
                //string inputText = "This movie is really good!";
                using (HttpClient client = new HttpClient())
                {
                    var parameters = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("q", review)
                    });
                    HttpResponseMessage response = await client.PostAsync(apiUrl, parameters);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        
                        var jsonOutput = response.Content.ReadAsStringAsync().Result;
                        dynamic result = JsonConvert.DeserializeObject(jsonOutput);
                        Sentiment_Category = (string)result.sentiment;                   
                    }
                    else
                    {
                        Console.WriteLine("Request failed with status code: " + response.StatusCode);
                    }
                }
            }   

            if (Seller_id == null)
            {
                return RedirectToAction("ReviewNotFound");
            }

            // get Seller to update his rate
            //Get All Users
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response0 = client.Get("Users");
            dynamic data0 = JsonConvert.DeserializeObject<dynamic>(response0.Body);
            var all_Users = new List<SignUpModel>();
            foreach (var item in data0)
            {
                all_Users.Add(JsonConvert.DeserializeObject<SignUpModel>(((JProperty)item).Value.ToString()));
            }

            foreach (var item in all_Users)
            {
                if (item.SignUp_ID == Seller_id)
                {
                    item.TotalReviews += 1;
                    if (Sentiment_Category.Equals("Positive"))
                    {
                        item.PositiveReviews += 1;
                    }
                    client.Set("Users/" + item.SignUp_ID, item);
                    break;
                }
            }
           
            //now we need to remove the seller from My_review { review only one time }
            //Get All Users2Ids
            client = new FireSharp.FirebaseClient(config);
            FirebaseResponse response3 = client.Get("Users2Ids");
            dynamic data = JsonConvert.DeserializeObject<dynamic>(response3.Body);
            var all_ReviewRate = new List<ReviewRate>();
            foreach (var item in data)
            {
                all_ReviewRate.Add(JsonConvert.DeserializeObject<ReviewRate>(((JProperty)item).Value.ToString()));
            }
            foreach (var item in all_ReviewRate)
            {
                if (item.SellerId== Seller_id)
                {
                    client = new FireSharp.FirebaseClient(config);
                    FirebaseResponse response4 = client.Delete("Users2Ids/" + item.ReviewRateId);
                }
            }
            return RedirectToAction("My_User_Review", "SentimentReviewRate");
        }
        //no user found
        public ActionResult ReviewNotFound()
        {
            return View();
        }
    }
}