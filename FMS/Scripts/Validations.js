
// ** Number Related **
//numbers only

    function Numtxtbox() 
    {
    if (!(event.keyCode == 48 || event.keyCode == 49 || event.keyCode == 50 || event.keyCode == 51 || event.keyCode == 52 || event.keyCode == 53 || event.keyCode == 54 || event.keyCode == 55 || event.keyCode == 56 || event.keyCode == 57))
      {
        event.returnValue = false;
      }
    }

    function Decimaltxtbox(txtbox, num)
     {

    if (!(event.keyCode == 46 || event.keyCode == 48 || event.keyCode == 49 || event.keyCode == 50 || event.keyCode == 51 || event.keyCode == 52 || event.keyCode == 53 || event.keyCode == 54 || event.keyCode == 55 || event.keyCode == 56 || event.keyCode == 57))
      {
          event.returnValue = false;
      }
      else 
      {
          var DigitsAfterDecimal = num;
          var val = txtbox.value;
          if (txtbox.value == "")
              val = String.fromCharCode(event.keyCode);
          else
              val = txtbox.value;

          if (val.indexOf(".") > -1) {
              if (event.keyCode == 46) {
                  alert("only one decimal point is allowed.");
                  event.returnValue = false;
                  return;
              }
              if (val.length - (val.indexOf(".") + 1) > DigitsAfterDecimal - 1) {
                  alert("Please enter valid decimal value.Only " + DigitsAfterDecimal + " digits are allowed after Decimal Pont.");
                  event.returnValue = false;
              }
              else {
                  event.returnValue = true;
              }
          }
      }
  }

  function isInteger(s)
   {
      var i;
      for (i = 0; i < s.length; i++) {
          // Check that current character is number.
          var c = s.charAt(i);
          if (((c < "0") || (c > "9"))) return false;
      }
      // All characters are numbers.
      return true;
  }

  function IsDecimal(str, num) {
      str = alltrim(str);
      return /^[-+]?\d{3,5}(\.\d{1,num})?$/.test(str);
  }


  function CheckUnit(value, DigitsAfterDecimal)
   {

       if (value == '') 
       {
           alert("Please enter valid decimal value");
           return false;
       }
       else {

           if (isNaN(value)) {
               alert("Please enter valid decimal value");
               return false;
           }
           else {
               var val = value;
               if (val.indexOf(".") > -1) {

                   if (val.length - (val.indexOf(".") + 1) > DigitsAfterDecimal) {
                       alert("Please enter valid decimal value. Only " + DigitsAfterDecimal + "digits are allowed after  decimal.");
                       return false;
                   }
                   else {
                       return true;
                   }
               }
               else {
                   if (parseInt(val) > 0) {
                       return true;
                   }
                   else {
                       alert("Value must be greater than 0");
                       return false;
                   }
               }
           }
       }
  }



  // ** Character Related **
        
    function SpecialChar()
    {
        if(!(event.keyCode==35||event.keyCode==38||event.keyCode==40||event.keyCode==41||event.keyCode==42||event.keyCode==44||event.keyCode==45||event.keyCode==46||event.keyCode==47||event.keyCode==64||event.keyCode==92||event.keyCode==95||event.keyCode==124||event.keyCode==63))
        {
            event.returnValue=false;
        }
    }

    function alphaOnly(eventRef)
    {
        var keyStroke = (eventRef.which) ? eventRef.which : (window.event) ? window.event.keyCode : -1;
        var returnValue = false;
        if ((keyStroke == 127) || (keyStroke == 08) || (keyStroke == 09) || (keyStroke == 32))
         return true;

        if (((keyStroke >= 65) && (keyStroke <= 90)) ||((keyStroke >= 97) && (keyStroke <= 122)))
         returnValue = true;

        if (navigator.appName.indexOf('Microsoft') != -1)
        window.event.returnValue = returnValue;

        return returnValue;
    }

    function stripCharsInBag(s, bag)
     {
        var i;
        var returnString = "";
        // Search through string's characters one by one.
        // If character is not in bag, append to returnString.
        for (i = 0; i < s.length; i++) {
            var c = s.charAt(i);
            if (bag.indexOf(c) == -1) returnString += c;
        }
        return returnString;
    }

    // ** Date Related **
                    
        var minYear=1900;
        var maxYear=2100;

           

            function daysInFebruary (year)
            {
            // February has 29 days in any year evenly divisible by four,
            // EXCEPT for centurial years which are not also divisible by 400.
            return (((year % 4 == 0) && ( (!(year % 100 == 0)) || (year % 400 == 0))) ? 29 : 28 );
            }
            function DaysArray(n)
            {
            for (var i = 1; i <= n; i++)
            {
                this[i] = 31
                if (i==4 || i==6 || i==9 || i==11) {this[i] = 30}
                if (i==2) {this[i] = 29}
            } 
            return this
            }

            function isDate(dtStr,dtCh,type)
            {
                
	            var daysInMonth = DaysArray(12)
	            var pos1
	            var pos2
	            var strMonth
	            var strDay
	            var strYear
	            if(type=='m')
	            {
	                 pos1=dtStr.indexOf(dtCh)
	                 pos2=dtStr.indexOf(dtCh,pos1+1)
	                 strMonth=dtStr.substring(0,pos1)
	                 strDay=dtStr.substring(pos1+1,pos2)
	                 strYear=dtStr.substring(pos2+1)
	            }
	            else
	            {
	                 pos1=dtStr.indexOf(dtCh)
	                 pos2=dtStr.indexOf(dtCh,pos1+1)
	                 strDay=dtStr.substring(0,pos1)
	                 strMonth=dtStr.substring(pos1+1,pos2)
	                 strYear=dtStr.substring(pos2+1)
	            }
	            strYr=strYear
	            if (strDay.charAt(0)=="0" && strDay.length>1) strDay=strDay.substring(1)
	            if (strMonth.charAt(0)=="0" && strMonth.length>1) strMonth=strMonth.substring(1)
	            for (var i = 1; i <= 3; i++)
	            {
		            if (strYr.charAt(0)=="0" && strYr.length>1) strYr=strYr.substring(1)
	            }
	            month=parseInt(strMonth)
	            day=parseInt(strDay)
	            year=parseInt(strYr)
	            if (pos1==-1 || pos2==-1)
	            {
	             if(type=='m')
	             {
	                if(dtCh=='/')
	                    alert("The date format should be : mm/dd/yyyy")
	                else
	                    alert("The date format should be : mm-dd-yyyy")
	             }
	             else
	             {
	                 if(dtCh=='/')
	                    alert("The date format should be : dd/mm/yyyy")
	                else
	                    alert("The date format should be : dd-mm-yyyy")
	             }
		            
		            return false
	            }
	            if (strMonth.length<1 || month<1 || month>12)
	            {
		            alert("Please enter a valid month")
		            return false
	            }
	            if (strDay.length<1 || day<1 || day>31 || (month==2 && day>daysInFebruary(year)) || day > daysInMonth[month])
	            {
		            alert("Please enter a valid day")
		            return false
	            }
	            if (strYear.length != 4 || year==0 || year<minYear || year>maxYear){
		            alert("Please enter a valid 4 digit year between "+minYear+" and "+maxYear)
		            return false
	            }
	            if (dtStr.indexOf(dtCh,pos2+1)!=-1 || isInteger(stripCharsInBag(dtStr, dtCh))==false)
	            {
		            alert("Please enter a valid date")
		            return false
	            }
              return true
          }
            
// ** Other Functions            

                function ValidateForm(txtbox, dtchar,type)
                {

                var dt = txtbox.value;
                if (isDate(dt,dtchar,type)==false)
                {
                    
                    return false
                }
                return true
            }
            
            //Tab Functionality Related Functions

            function clickButton(e, buttonid) {

                var evt = e ? e : window.event;
                var bt = document.getElementById(buttonid);

                if (bt) {
                    if (evt.keyCode == 9) {
                        bt.focus();
                        return false;
                    }
                }
            }

            //Text Box Related Functions

            function removespaces(eventRef, obj) {
                var txt = document.getElementById(obj);
                var CharArray = txt.value.split('');
                //var CharArray=txt.toString().split('');
                //var CharArray=document.forms.item[txt].value.split('');
                for (var i = 0; i < CharArray.length; i++) {
                    if (CharArray[i] == " ") {
                        if (CharArray[i + 1] == " ") {
                            //alert("Error, there must be one space char between words..!");
                            //txt.value = txt.value.replace (" ", "");            
                            txt.value = txt.value.substring(0, txt.value.length - 1);
                            return false;
                        }
                    }
                }
                return true;
            }
            

            function checkInputLength(eventRef, obj) {
                var objtextarea = document.getElementById(obj);
                if (objtextarea.value.length >= 4000) {
                    window.event.returnValue = false;
                    return false;
                }
                else {
                    return true;
                }
            }
            function validateDateAndTime(strDate) {
                var strErr = "";
                var valid = 1;
                var mdate = strDate.replace(/-/g, "/");
                strDate = mdate;
              
                var first = strDate.indexOf('/');
               
                if ((first == -1)) {
                    strErr = "Date should be in dd/MM/yyyy format";
                    return strErr;
                }
                var d = strDate.substr(0, first);
               
                if (d != parseInt(d, 10)) {
                    strErr = "Date should be in dd/MM/yyyy format";
                    return strErr;
                }
                var tempmonth = strDate.substr(first + 1);
                var second = tempmonth.indexOf('/');
                if ((second == -1)) {
                    strErr = "Date should be in dd/MM/yyyy format";
                    return strErr;
                }
                var m = tempmonth.substr(0, second);
               
                if (m != parseInt(m, 10)) {
                    strErr = "Valid Month is required";
                    return strErr;
                }

                var tempyear = strDate.substr(second + 4);
                var third = tempyear.indexOf(' ');
               if ((third == -1)) {
                    strErr = "Enter space between year and time";
                    return strErr;
                }
                var y = tempyear.substr(0, third);
               
                if (y != parseInt(y, 10)) {
                    strErr = "Valid Year is required";
                    return strErr;
                }
                var temphours = strDate.substr(third + 6);
                var fourth = tempyear.indexOf(':');
                if ((fourth == -1)) {
                    strErr = "Invalid Time format";
                    return strErr;
                }
                if (temphours != "") {
                    
                    var sHours = temphours.split(':')[0];
                    var sMinutesandAmPm = temphours.split(':')[1];

                    var spaceIdex = sMinutesandAmPm.indexOf(' ');
                    if ((spaceIdex == -1)) {
                        strErr = "Enter space between Minutes and AM/PM or Please mention AM/PM";
                        return strErr;
                    }
                    var sMinutes = sMinutesandAmPm.substr(0, spaceIdex);
                    var ampm = sMinutesandAmPm.substr(spaceIdex + 1);
                   
                }
                var yl = 1900; // least year to consider
                var ym = 2999; // most year to consider
                if ((m < 1 || m > 12)) {
                    strErr = "Month must be in between 1 and 12";
                  return strErr;
                }
                if ((d < 1 || d > 31)) {
                    strErr = "Day must be in between 1 and 31";
                   return strErr;
                }
                if ((y < yl || y > ym)) {
                    strErr = "Year must be in between 1900 and 2999";
                    return strErr;
                }
                if (m == 4 || m == 6 || m == 9 || m == 11)
                    if ((d == 31)) {
                        strErr = "This month consists maximum of 30 days";
                       return strErr;
                    }
                if (m == 2) {
                    var b = parseInt(y / 4, 10);
                    if (isNaN(b)) {
                        strErr = "Enter valid date";
                        return strErr;
                    }
                    if (d > 29) {
                        strErr = "Feb consists maximum of 29 days";
                       return strErr
                    }
                    if (d == 29 && ((y / 4) != parseInt(y / 4, 10))) {
                        strErr = "Feb consists maximum of 28 days";
                       return strErr;
                    }
                    if (d == 29 && ((y / 4) == parseInt(y / 4, 10))) {
                        if (y % 100 == 0) {
                            if (y % 400 != 0) {
                                strErr = "Feb consists maximum of 28 days";
                               return strErr;
                            }
                        }
                    }
               }
               if (sHours == "" || isNaN(sHours) || parseInt(sHours) > 12) {
                   strErr = "Hours must be in between 1 and 12";
                   return strErr;
               }

               if (sMinutes == "" || isNaN(sMinutes) || parseInt(sMinutes) > 59) {
                   strErr = "Minutes must be in between 1 and 59";
                   return strErr;
               }
               
                return strErr;
            }


            function validateTime(time) {
                var timeValue = obj.value;
                if (timeValue == "" || timeValue.indexOf(":") < 0) {
                    strErr = "Invalid Time format";
                    return strErr;
                }
                else {
                    var sHours = timeValue.split(':')[0];
                    var sMinutes = timeValue.split(':')[1];

                    if (sHours == "" || isNaN(sHours) || parseInt(sHours) > 23) {
                        strErr = "Hours must be in between 1 and 23";
                        return strErr;
                    }
                    else if (parseInt(sHours) == 0)
                        sHours = "00";
                    else if (sHours < 10)
                        sHours = "0" + sHours;

                    if (sMinutes == "" || isNaN(sMinutes) || parseInt(sMinutes) > 59) {
                        strErr = "Minutes must be in between 1 and 59";
                        return strErr;
                    }
                    else if (parseInt(sMinutes) == 0)
                        sMinutes = "00";
                    else if (sMinutes < 10)
                        sMinutes = "0" + sMinutes;

                    obj.value = sHours + ":" + sMinutes;
                }

                return true;
            }

            function validateDate(strDate) {
                var strErr = "";
                var valid = 1;
                var mdate = strDate.replace(/-/g, "/");
                strDate = mdate;
                var first = strDate.indexOf('/');
                if ((first == -1)) {
                    strErr = "Date should be in dd/MM/yyyy format";
                    //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Valid Date is required";

                    return strErr;
                }
                var d = strDate.substr(0, first);

                if (d != parseInt(d, 10)) {
                    strErr = "Date should be in dd/MM/yyyy format";
                    //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Valid Date is required";

                    return strErr;
                }
                var tempmonth = strDate.substr(first + 1);
                var second = tempmonth.indexOf('/');
                if ((second == -1)) {
                    strErr = "Date should be in dd/MM/yyyy format";
                    //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Valid Date is required";

                    return strErr;
                }
                var m = tempmonth.substr(0, second);
                if (m != parseInt(m, 10)) {
                    strErr = "Valid Month is required";
                    // document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Valid Month is required";

                    return strErr;
                }
                var y = tempmonth.substr(second + 1);
                if (y != parseInt(y, 10)) {
                    strErr = "Valid Year is required";
                    //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Valid Year is required";

                    return strErr;
                }
                var yl = 1900; // least year to consider
                var ym = 2999; // most year to consider
                if ((m < 1 || m > 12)) {
                    strErr = "Month must be in between 1 and 12";
                    //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Month must be in between 1 and 12"; //Month must be in between 1 and 12

                    return strErr;
                }
                if ((d < 1 || d > 31)) {
                    strErr = "Day must be in between 1 and 31";
                    //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Day must be in between 1 and 31"; //Day must be in between 1 and 31

                    return strErr;
                }
                if ((y < yl || y > ym)) {
                    strErr = "Year must be in between 1900 and 2999";
                    //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Year must be in between 1900 and 2999"; //Year must be in between 1900 and 2999

                    return strErr;
                }
                if (m == 4 || m == 6 || m == 9 || m == 11)
                    if ((d == 31)) {
                        strErr = "This month consists maximum of 30 days";
                        // document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "This month consists maximum of 30 days"; //Month consists maximum of 30 days

                        return strErr;
                    }
                if (m == 2) {
                    var b = parseInt(y / 4, 10);
                    if (isNaN(b)) {
                        strErr = "Enter valid date";
                        //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Enter valid date"

                        return strErr;
                    }
                    if (d > 29) {
                        strErr = "Feb consists maximum of 29 days";
                        //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Feb consists maximum of 29 days";

                        return strErr
                    }
                    if (d == 29 && ((y / 4) != parseInt(y / 4, 10))) {
                        strErr = "Feb consists maximum of 28 days";
                        //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Feb consists maximum of 28 days";
                        //alert('Feb consists maximum of 28 days');

                        return strErr;
                    }
                    if (d == 29 && ((y / 4) == parseInt(y / 4, 10))) {
                        if (y % 100 == 0) {
                            if (y % 400 != 0) {
                                strErr = "Feb consists maximum of 28 days";
                                //document.getElementById('ctl00_MainContent_lblValidation').innerHTML = "Feb consists maximum of 28 days";

                                return strErr;
                            }
                        }
                    }
                }
                return strErr;
            } 