using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace econest.Controllers
{
    public class paymentController : Controller
    {
        SqlConnection con = new SqlConnection("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\Shweta\\Documents\\econestdb.mdf;Integrated Security=True;Connect Timeout=30");

        private readonly string razorpayKey = "rzp_test_3qGEjts0bLqiFo";
        private readonly string razorpaySecret = "0r6qwPetotnVqdV0N7D0rmJy";

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> checkout()
        {
            string uidStr = Request.Cookies["uid"];
            if (string.IsNullOrEmpty(uidStr))
            {
                TempData["msg"] = "User not logged in.";
                return RedirectToAction("Login", "User");
            }

            int uid = Convert.ToInt32(uidStr);
            decimal total = 0;

            con.Open();

            SqlCommand getCartCmd = new SqlCommand(@"
                SELECT p.price, c.qty 
                FROM tblcart c
                INNER JOIN tblproduct p ON c.pid = p.pid
                WHERE c.uid = @uid", con);
            getCartCmd.Parameters.AddWithValue("@uid", uid);

            var dr = getCartCmd.ExecuteReader();
            while (dr.Read())
            {
                decimal price = Convert.ToDecimal(dr["price"]);
                int qty = Convert.ToInt32(dr["qty"]);
                total += price * qty;
            }
            dr.Close();
            con.Close();

            int amountInPaise = (int)(total * 100);

            // Create Razorpay order using HttpClient
            var client = new HttpClient();

            // Basic Auth header (key:secret base64 encoded)
            var authToken = Encoding.ASCII.GetBytes($"{razorpayKey}:{razorpaySecret}");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

            var orderPayload = new
            {
                amount = amountInPaise,
                currency = "INR",
                payment_capture = 1
            };

            var jsonPayload = JsonSerializer.Serialize(orderPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
var response = await client.PostAsync("https://api.razorpay.com/v1/orders", content);
            if (!response.IsSuccessStatusCode)
            {
                // Handle error, e.g., show message
                TempData["msg"] = "Failed to create payment order. Please try again.";
                return RedirectToAction("Index");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            var orderId = doc.RootElement.GetProperty("id").GetString();

            ViewBag.RazorpayKey = razorpayKey;
            ViewBag.RazorpayOrderId = orderId;
            ViewBag.Amount = amountInPaise;
            ViewBag.AmountInRupees = total;

            return View();
        }

        public IActionResult GetCreditCardView()
        {
            return PartialView("GetCreditCardView");
        }
        public IActionResult GetNetBankingView()
        {
            return PartialView("GetNetBankingView");
        }
        public IActionResult GetCashOnDeliveryView()
        {
            return PartialView("GetCashOnDeliveryView");
        }
    }
}
