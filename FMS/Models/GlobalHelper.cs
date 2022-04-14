using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FMS.Models;
 

namespace FMS.Models 
{
    public static class GlobalHelper
    {


        public static DateTime Toddmmyy(string date)
        {
            DateTime _toddmmyy;
            string[] tDate = date.Split('/');
            _toddmmyy = DateTime.Parse(tDate[1] + "/" + tDate[0] + "/" + tDate[2]);  //tDate[1] + "/" + tDate[0] + "/" + tDate[2].Replace("12:00:00 AM", "");
            return _toddmmyy;
        }
        //public static DateTime Tommddyy(string date)
        //{
        //    DateTime _tommddyy;
        //    string[] tDate = date.Split('/');
        //    _tommddyy = DateTime.Parse(tDate[1] + "/" + tDate[0] + "/" + tDate[2]);
        //    return _tommddyy;
        //}

        public static DateTime Tommddyy(DateTime customDate)
        {
            DateTime _tommddyy;
            int lastday = customDate.Day;
            int month = customDate.Month;
            int year = customDate.Year;
            _tommddyy = Convert.ToDateTime(month + "/" + lastday + "/" + year);
            return _tommddyy;
        }

        //public static List<System.Nullable<int>> GetCustIdsByUserName(string username)
        //{
        //    FMSDBEntities db = new FMSDBEntities();

        //    List<System.Nullable<int>> custids;
        //    try
        //    {
        //        Guid userid = db.aspnet_Users.Where(u => u.UserName == username).SingleOrDefault().UserId;
        //        custids =
        //         (from c in db.user_cust
        //         where c.UserId==userid
        //         select c.cid).ToList();
        //    }
        //    catch(Exception ex)
        //    { custids = null; }

            
        //    return custids;
        //}
    }
}