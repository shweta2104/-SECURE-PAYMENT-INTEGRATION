using econest.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
namespace econest.Controllers
{
    public class orderController : Controller
    {
        SqlConnection con = new SqlConnection("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\Shweta\\Documents\\econestdb.mdf;Integrated Security=True;Connect Timeout=30");
        SqlCommand cmd = new SqlCommand();
        SqlDataAdapter adp = new SqlDataAdapter();
        DataSet ds = new DataSet();
        DataTable dt = new DataTable();
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult orderin()
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

            SqlDataReader dr = getCartCmd.ExecuteReader();
            while (dr.Read())
            {
                decimal price = Convert.ToDecimal(dr["price"]);
                int qty = Convert.ToInt32(dr["qty"]);
                total += price * qty;
            }
            dr.Close();
            con.Close();

            ViewBag.Total = total;

            return View();
        }


       

        [HttpPost]
        public IActionResult orderin(tblorder o)
        {
            try
            {
                string uidStr = Request.Cookies["uid"];
                if (string.IsNullOrEmpty(uidStr))
                {
                    TempData["msg"] = "User not logged in.";
                    return RedirectToAction("Login", "User");
                }

                int uid = Convert.ToInt32(uidStr);

                 //string fullAddress = Request.Form["address1"] + ", " +
                 //            Request.Form["address2"] + ", " +
                 //            Request.Form["state"] + " - " +
                 //            Request.Form["pincode"];


                decimal total = 0;
                List<int> productIds = new List<int>();

                con.Open();

                // Step 1: Get all products in cart with price and quantity
                SqlCommand getCartCmd = new SqlCommand(@"
            SELECT c.pid, p.price, c.qty 
            FROM tblcart c
            INNER JOIN tblproduct p ON c.pid = p.pid
            WHERE c.uid = @uid", con);
                getCartCmd.Parameters.AddWithValue("@uid", uid);

                SqlDataReader dr = getCartCmd.ExecuteReader();
                while (dr.Read())
                {
                    int pid = Convert.ToInt32(dr["pid"]);
                    decimal price = Convert.ToDecimal(dr["price"]);
                    int qty = Convert.ToInt32(dr["qty"]);
                    total += price * qty;
                    productIds.Add(pid);
                }
                dr.Close();

                // Step 2: Insert each order (1 row per product)
                foreach (int pid in productIds)
                {
                    // Fetch product name for this pid
                    SqlCommand getProductNameCmd = new SqlCommand("SELECT pname FROM tblproduct WHERE pid = @pid", con);
                    getProductNameCmd.Parameters.AddWithValue("@pid", pid);
                    string productName = (string)getProductNameCmd.ExecuteScalar();

                    SqlCommand insertCmd = new SqlCommand(@"
        INSERT INTO tblorder 
        (uid, pid, name, o_date, total_amt, p_status, p_method, address, o_status, contact) 
        VALUES 
        (@uid, @pid, @name, @o_date, @total_amt, @p_status, @p_method, @address, @o_status, @contact)", con);

                    insertCmd.Parameters.AddWithValue("@uid", uid);
                    insertCmd.Parameters.AddWithValue("@pid", pid);
                    insertCmd.Parameters.AddWithValue("@name", productName);  // Use product name here
                    insertCmd.Parameters.AddWithValue("@o_date", DateTime.Now);
                    insertCmd.Parameters.Add("@total_amt", SqlDbType.Decimal).Value = Math.Round(total, 2);
                    insertCmd.Parameters.AddWithValue("@p_status", "Pending"); // default status
                    insertCmd.Parameters.AddWithValue("@p_method", o.p_method);
                    insertCmd.Parameters.AddWithValue("@address", o.address);
                    insertCmd.Parameters.AddWithValue("@o_status", "Pending");
                    insertCmd.Parameters.AddWithValue("@contact", o.contact);

                    insertCmd.ExecuteNonQuery();
                }

                // Step 3: Clear user's cart after placing order
                SqlCommand clearCartCmd = new SqlCommand("DELETE FROM tblcart WHERE uid = @uid", con);
                clearCartCmd.Parameters.AddWithValue("@uid", uid);
                clearCartCmd.ExecuteNonQuery();

                TempData["msg"] = "Order placed successfully!";
                return RedirectToAction("success");
            }
            catch (Exception ex)
            {
                TempData["msg"] = "Error placing order: " + ex.Message;
                return RedirectToAction("cartitem", "Cart");
            }
            finally
            {
                con.Close();
            }
        }

        public IActionResult success()
        {
            return View();
        }

        public IActionResult history()
        {
            string uidStr = Request.Cookies["uid"];

            if (string.IsNullOrEmpty(uidStr) || !int.TryParse(uidStr, out int uid))
            {
                return RedirectToAction("Login", "Account");
            }

            List<tblorder> orders = new List<tblorder>();

            con.Open();
            //SqlCommand cmd = new SqlCommand("SELECT * FROM tblorder WHERE uid = @uid ORDER BY o_date DESC", con);
            SqlCommand cmd = new SqlCommand(
    "SELECT o.*, p.pname FROM tblorder o " +
    "JOIN tblproduct p ON o.pid = p.pid " +
    "WHERE o.uid = @uid ORDER BY o.o_date DESC", con);

            cmd.Parameters.AddWithValue("@uid", uid);
            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                tblorder o = new tblorder
                {
                    oid = Convert.ToInt32(dr["oid"]),
                    uid = Convert.ToInt32(dr["uid"]),
                    pid = Convert.ToInt32(dr["pid"]),
                    name = dr["pname"].ToString(),
                    o_date = Convert.ToDateTime(dr["o_date"]),
                    total_amt = Convert.ToDecimal(dr["total_amt"]),
                    p_status = dr["p_status"].ToString(),
                    p_method = dr["p_method"].ToString(),
                    address = dr["address"].ToString(),
                    o_status = dr["o_status"].ToString(),
                    contact = dr["contact"].ToString(),
                };

                orders.Add(o);
            }

            dr.Close();
            con.Close();

            return View(orders);
        }


    }
}
