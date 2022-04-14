/* LastMile IT systems */

function isValidCurrency(currency) {
    var pattern = new RegExp(/^\d+(?:\.\d{0,4})$/);
    return pattern.test(currency);
};
function isValidRupees(currency) {
    var pattern = new RegExp(/^([0-9\,\.])+$/);
    return pattern.test(currency);
};
function isAlphaNumeric(alphaNumericVal) {
    var pattern = new RegExp(/^([A-Za-z0-9_-]|\s)+$/);
    return pattern.test(alphaNumericVal);
};

function isAlphaNumericWithSpace_Comma(alphaNumericVal) {
    var pattern = new RegExp(/^([A-Za-z0-9\-\s\,_])+$/);
    return pattern.test(alphaNumericVal);
};

function isPureAlphaNumeric(alphaNumericVal) {
    var pattern = new RegExp(/^([A-Za-z0-9])+$/);
    return pattern.test(alphaNumericVal);
};

function isName(nameVal) {
    var pattern = new RegExp(/^([a-zA-Z0-9\,\'\-\s\/\.&_])+$/);
    return pattern.test(nameVal);
};

function isAlphaFewChars(alphaVal) {
    var pattern = new RegExp(/^([a-zA-Z\,\s\/\.&])+$/);
    return pattern.test(alphaVal);
};

function isInvalidChars(invalidVal) {
    var pattern = new RegExp(/[^~|!|#|\$|\^|\*|\(|\)|\=|\+|\{|\}|\[|\]|\||\:|\;|\"|\'|\<|\>|\?]/);
    return pattern.test(invalidVal);
};

function isExperience(experienceVal) {
    var pattern = new RegExp(/^([a-zA-Z0-9\,\s\/\.\(\)\=\+\{\}\[\]\<\>\-&_])+$/);
    return pattern.test(experienceVal);
};

function isCriteriaDetail_isValid(nameVal) {
    var pattern = new RegExp(/^[a-zA-Z0-9]([a-zA-Z0-9\,\s\?\-\/\.&])+$/);
    return pattern.test(nameVal);
};

function isAlpha(alphaVal) {
    var pattern = new RegExp(/^[a-zA-Z\s\-]*$/);
    return pattern.test(alphaVal);
};
function isAlphaStart_isValid(nameVal) {
    var pattern = new RegExp(/^[a-zA-Z]([a-zA-Z0-9\,\-\s\/\.&])+$/);
    return pattern.test(nameVal);
};
function isNumeric(numericVal) {
    // var pattern = new RegExp(/^[0-9]*$/);
    var pattern = new RegExp(/^[0-9][0-9]*$/);
    return pattern.test(numericVal);
};
function isNumerics(numericVal) {
    var pattern = new RegExp(/^[1-9][0-9]*$/);
    return pattern.test(numericVal);
};
function isNumerical(numericVal) {
    var pattern = new RegExp(/^[0-9]*$/);
    return pattern.test(numericVal);
};
function IsNumericTime(numericVal) {
    var pattern = new RegExp(/^[0-9\:]*$/);
    return pattern.test(numericVal);
}
function isFloat(floatVal) {
    var pattern = new RegExp(/^(?:[1-9]\d*|0)?(?:\.\d+)?$/);
    return pattern.test(floatVal);
};

function isValidDate(dateVal) { /* dd/mm/yy format */
    var pattern = new RegExp(/^[0-9]{4}\-(0[1-9]|1[012])\-(0[1-9]|[12][0-9]|3[01])/);
    return pattern.test(dateVal);
    return true;
};

function isValidEmailAddress(emailAddress) {
    var pattern = new RegExp(/^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$/i);
    return pattern.test(emailAddress);
};

//25-01-2016 by vinod
function isNames(nameVal) {
    var pattern = new RegExp(/^[a-zA-Z,. ]*$/);
    return pattern.test(nameVal);
};

function isValidEmailAddress1(emailAddress) {
    var pattern = new RegExp(/[\w+-.%]+@[\w-.]+\.[A-Za-z]{2,4},?/);
    return pattern.test(emailAddress);
};
//end


//By sarath  VehRegNumber Validation 23-01-16

function isVehRegNumber(vehregnumber) {
    var pattern = new RegExp(/^([A-Z|a-z]{2}\s{1}\d{2}\s{1}[A-Z|a-z]{1,3}\s{1}\d{1,4})?([A-Z|a-z]{3}\s{1}\d{1,4})?$/);
    return pattern.test(vehregnumber);
};

//25-01-2016 by sarath
function isName1(nameVal) {
    var pattern = new RegExp(/^([a-zA-Z\,\'\-\s\/\.&_])+$/);
    return pattern.test(nameVal);
};
function isMobileNumber(mobilenumber) {
    var pattern = new RegExp(/^\d{10,10}(,\d{10,10})*$/);
    return pattern.test(mobilenumber);
};

function isValidMobileNumber(mobileNumber) {
    //var pattern = new RegExp(/[0-9]{10}/);
    var pattern = new RegExp(/^\+?([0-9\s\/\-]{10,10})+$/);
    return pattern.test(mobileNumber);
};
//exists
//function isValidMobileNumber(mobileNumber) {
//    //var pattern = new RegExp(/[0-9]{10}/);
//    var pattern = new RegExp(/^\+?([0-9\s\/\-])+$/);
//    return pattern.test(mobileNumber);
//};

//end
function isLocalPhone(mobileNum) {
    var pattern = mobileNum;
    if (pattern[0] == 0 && pattern.length == 10) {
        pattern = pattern.replace('0', '255');
    }
    return pattern;
};

function isContainsSpecialChar(stringVal) {
    var pattern = new RegExp(/[-[\]\/{}()*+?.\\^$|]/);
    return pattern.test(stringVal);
};
function isValidWebsite(wbsite) {
    var pattern = new RegExp(/(w{3})\.([a-zA-Z]+)\/?[a-z]*\.([a-z]{2,4})$/);
    return pattern.test(wbsite);
}
/*function isValidPassword(pwd) {
var pattern = new RegExp(/^.*(?=.{8,})(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z])(^[a-zA-Z0-9@$=!:.#%]+)$/);
return pattern.test(pwd);
};*/
function isValidPassword(pwd) {
    var pattern = new RegExp(/^([a-zA-Z0-9@$=!:.#%])+$/);
    return pattern.test(pwd);
};

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

//newly added Validation functions
 function IsPureCharsAndSpace(value) 
    {
       // var check = /^([a-zA-Z]+\s?)*$/; this allow only Space at a time
        var check = /^([a-zA-Z]+\s)*[a-zA-Z]+$/
        if(!check.test(value)) 
        {
           return false;
        }
        else
        {
           return true;
        }    
    } 
     function IsPureCharsAndHifen(value) 
    {
       //only one -(Hifen) inside string not at beginning or end
       var check=/^[a-zA-Z]+([\-]?[a-zA-Z]+)?$/
        if(!check.test(value)) 
        {
           return false;
        }
        else
        {
           return true;
        }    
    } 
 function IsEmail(email) 
   {
        var regex = /^([a-zA-Z0-9_\.\-\+])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$/;
        if(!regex.test(email)) 
        {
           return false;
        }
        else
        {
           return true;
        }
   }
   function DisableSpace(e) {
       var input = e.currentTarget.value;
       if (e.which == 32 && !input.length) {
           return false;
       }
       else {
           while (input.substring(0, 1) === ' ') {   // First character is 'space'.
               return false;
               //input = input.substring(1);             // Trim the leading 'space'
           }
           $(e.currentTarget).val(input);   // update input with new value
       }
   }

   function DisableFirstCharZero(e) {
       var input = e.currentTarget.value;
       if (input == 0 && e.which == 48 || e.which < 48 || e.which > 57) {
           return false;
       }
       else {
           while (input.substring(0, 1) === '0') {   // First character is a '0'.
               return false;
               //input = input.substring(1);             // Trim the leading '0'
           }
           $(e.currentTarget).val(input);   // update input with new value
       }
       if (e.keyCode == 32) {
           e.returnValue = false;
           return false;
       }
   }