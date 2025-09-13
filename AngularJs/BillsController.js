/// <reference path="angular.min.js" />
/// <reference path="NumberFormatter.js" />

var myApp = angular.module("myModule", [])
                   .config(function ($locationProvider) {
                       $locationProvider.html5Mode({
                           enabled: true,
                           requireBase: false
                       });
                   }).directive("sgNumberInput", sgNumberInput);

//var myApp = angular.module("myModule", []);

function sgNumberInput($filter, $locale) {
    //#region helper methods
    function getCaretPosition(inputField) {
        // Initialize
        var position = 0;
        // IE Support
        if (document.selection) {
            inputField.focus();
            // To get cursor position, get empty selection range
            var emptySelection = document.selection.createRange();
            // Move selection start to 0 position
            emptySelection.moveStart('character', -inputField.value.length);
            // The caret position is selection length
            position = emptySelection.text.length;
        }
        else if (inputField.selectionStart || inputField.selectionStart === 0) {
            position = inputField.selectionStart;
        }
        return position;
    }
    function setCaretPosition(inputElement, position) {
        if (inputElement.createTextRange) {
            var range = inputElement.createTextRange();
            range.move('character', position);
            range.select();
        }
        else {
            if (inputElement.selectionStart) {
                inputElement.focus();
                inputElement.setSelectionRange(position, position);
            }
            else {
                inputElement.focus();
            }
        }
    }
    function countNonNumericChars(value) {
        return (value.match(/[^a-z0-9]/gi) || []).length;
    }
    //#endregion helper methods



    return {
        require: "ngModel",
        restrict: "A",
        link: function ($scope, element, attrs, ctrl) {
            var fractionSize = parseInt(attrs['fractionSize']) || 0;
            var numberFilter = $filter('number');
            //format the view value
            ctrl.$formatters.push(function (modelValue) {
                var retVal = numberFilter(modelValue, fractionSize);
                var isValid = !isNaN(modelValue);
                ctrl.$setValidity(attrs.name, isValid);
                return retVal;
            });
            //parse user's input
            ctrl.$parsers.push(function (viewValue) {
                var caretPosition = getCaretPosition(element[0]), nonNumericCount = countNonNumericChars(viewValue);
                viewValue = viewValue || '';
                //Replace all possible group separators
                var trimmedValue = viewValue.trim().replace(/,/g, '').replace(/`/g, '').replace(/'/g, '').replace(/\u00a0/g, '').replace(/ /g, '');
                //If numericValue contains more decimal places than is allowed by fractionSize, then numberFilter would round the value up
                //Thus 123.109 would become 123.11
                //We do not want that, therefore I strip the extra decimal numbers
                var separator = $locale.NUMBER_FORMATS.DECIMAL_SEP;
                var arr = trimmedValue.split(separator);
                var decimalPlaces = arr[1];
                if (decimalPlaces !== null && decimalPlaces.length > fractionSize) {
                    //Trim extra decimal places
                    decimalPlaces = decimalPlaces.substring(0, fractionSize);
                    trimmedValue = arr[0] + separator + decimalPlaces;
                }
                var numericValue = parseFloat(trimmedValue);
                var isEmpty = numericValue === null || viewValue.trim() === "";
                var isRequired = attrs.required || false;
                var isValid = true;
                if (isEmpty && isRequired || !isEmpty && isNaN(numericValue)) {
                    isValid = false;
                }
                ctrl.$setValidity(attrs.name, isValid);
                if (!isNaN(numericValue) && isValid) {
                    var newViewValue = numberFilter(numericValue, fractionSize);
                    element.val(newViewValue);
                    var newNonNumbericCount = countNonNumericChars(newViewValue);
                    var diff = newNonNumbericCount - nonNumericCount;
                    var newCaretPosition = caretPosition + diff;
                    if (nonNumericCount === 0 && newCaretPosition > 0) newCaretPosition--;
                    setCaretPosition(element[0], newCaretPosition);
                }
                return !isNaN(numericValue) ? numericValue : null;
            });
        } //end of link function
    };
}

sgNumberInput.$inject = ["$filter", "$locale"];

myApp.controller("myController", function ($scope, $http, $filter) {

    //var phoneNoRegex = "(^[0-9]{10}$)|(^\+[0-9]{3}\s+[0-9]{3}[0-9]{6}$)|(^[0-9]{3}\s+[0-9]{3}[0-9]{6}$)";
    var phoneNoRegex = "/^\d{10}$/";
    $scope.phoneNoRegex = phoneNoRegex;

    $http({
        method: "GET",
        url: "/../ifmistz/Bills/GetCostings"
    }).then(function (response) {
        $scope.costings = response.data;
    });

    $http({
        method: "GET",
        url: "/../ifmistz/Bills/GetIdTypes"
    }).then(function (response) {
        $scope.idTypes = response.data;
    });

    var getCosting = function (id) {
        $http({
            method: "GET",
            url: "/../ifmistz/Bills/GetCosting",
            params: { id: id }
        }).then(function (response) {
            $scope.CostCode = response.data.CostCode,
            $scope.CostName = response.data.CostName,
            $scope.CostDesc = response.data.CostDesc,
            $scope.UOM = response.data.UOM,
            $scope.FinancialYearId = response.data.FinancialYearId,
            $scope.Currency = response.data.Currency,
            $scope.InstitutionId = response.data.InstitutionId,
            $scope.BillValidDays = response.data.BillValidDays,
            $scope.IsMainSource = response.data.IsMainSource,
            $scope.GLAccountReceivable = response.data.GLAccountReceivable,
            $scope.GLAccountRevenue = response.data.GLAccountRevenue,
            $scope.BillPayOption = response.data.BillPayOption,
            $scope.IsPreRevenue = response.data.IsPreRevenue,
            $scope.SubCostings = response.data.SubCostings,
            $scope.Currencies = response.data.Currencies
        });
    };
    $scope.getCosting = getCosting;

    var getSubCosting = function (id) {
        $http({
            method: "GET",
            url: "/../ifmistz/Bills/GetSubCosting",
            params: { id: id }
        }).then(function (response) {
            $scope.SubCostName = response.data.SubCostName,
            $scope.SubCostDesc = response.data.SubCostDesc,
            $scope.UnitCost = response.data.UnitCost
        });
    };
    $scope.getSubCosting = getSubCosting;

    var getCustomer = function (id) {
        $http({
            method: "GET",
            url: "/../ifmistz/Bills/GetCustomer",
            params: { id: id }
        }).then(function (response) {
            $scope.CustomerId = response.data.CustomerId;
            $scope.CustomerName = response.data.CustomerName;
            $scope.IdTypeId = response.data.IdTypeId;
            $scope.IdType = response.data.IdType;
            $scope.IdNo = response.data.IdNo;
            $scope.PhoneNo = response.data.PhoneNo;
            $scope.EmailAddress = response.data.EmailAddress;
        });
    };
    $scope.getCustomer = getCustomer;

    var getIdType = function (id) {
        $http({
            method: "GET",
            url: "/../ifmistz/Bills/GetIdType",
            params: { id: id }
        }).then(function (response) {
            $scope.IdType = response.data;
        });
    };
    $scope.getIdType = getIdType;

    var getOperationalAmount = function (para1, para2, hasVat) {
        if (isNaN(para1) || para1 === undefined) {
            para1 = 0;
        }
        if (isNaN(para2) || para2 === undefined) {
            para2 = 0;
        }
        var num1 = para1.toString().replace(/,/g, "");
        var num2 = para2.toString().replace(/,/g, "");        
        $scope.NetAmount = parseFloat(num1) * parseFloat(num2);

        if (hasVat) {
            $http({
                method: "GET",
                url: "/../ifmistz/Bills/GetVatDetails"
            }).then(function (response) {
                $scope.VatPercentage = response.data.VatPercentage;
                $scope.GlAccountVat = response.data.GlAccountVat;
                $scope.VatAmount = $scope.NetAmount * ($scope.VatPercentage / 100);
                $scope.OperationalAmount = $scope.NetAmount + ($scope.NetAmount * ($scope.VatPercentage / 100));
            });
        } else {
            $scope.VatPercentage = 0;
            $scope.VatAmount = 0;
            $scope.OperationalAmount = $scope.NetAmount + $scope.VatAmount;
        }
    };

    $scope.getOperationalAmount = getOperationalAmount;

    var getVatDetails = function getVatDetails(netAmount) {
        $http({
            method: "GET",
            url: "/../ifmistz/Bills/GetVatDetails"
        }).then(function (response) {
            $scope.VatPercentage = response.data.VatPercentage;
            $scope.GlAccountVat = response.data.GlAccountVat;
            $scope.VatAmount = netAmount * ($scope.VatPercentage / 100);
        });
    }

    $scope.createBill = function (isValid) {
        $scope.submitted = true;
        if (isValid) {
            $http({
                method: "POST",
                url: "/../ifmistz/Bills/CreateBill",
                data: {
                    CostingId: $scope.CostingId,
                    CostCode: $scope.CostCode,
                    CostName: $scope.CostName,
                    CostDesc: $scope.CostDesc,
                    UOM: $scope.UOM,
                    BillValidDays: $scope.BillValidDays,
                    IsMainSource: $scope.IsMainSource,
                    GLAccountReceivable: $scope.GLAccountReceivable,
                    GLAccountRevenue: $scope.GLAccountRevenue,
                    SubCostingId: $scope.SubCostingId,
                    SubCostName: $scope.SubCostName,
                    SubCostDesc: $scope.SubCostDesc,
                    BilledItemId: $scope.BilledItemId,
                    BilledItemDesc: $scope.BilledItemDesc,
                    CustomerId: $scope.CustomerId,
                    CustomerName: $scope.CustomerName,
                    PhoneNo: $scope.PhoneNo,
                    EmailAddress: $scope.EmailAddress,
                    IdTypeId: $scope.IdTypeId,
                    IdType: $scope.IdType,
                    IdNo: $scope.IdNo,
                    UnitCost: $scope.UnitCost,
                    Quantity: $scope.Quantity,
                    OperationalAmount: $scope.OperationalAmount,
                    OperationalCurrency: $scope.Currency,
                    TxtCustomerName: $scope.CustomerName,
                    DdlIdTypeId: $scope.IdTypeId,
                    HasVat: $scope.HasVat,
                    VatPercentage: $scope.VatPercentage,
                    VatAmount: $scope.VatAmount,
                    NetAmount: $scope.NetAmount,
                    GlAccountVat: $scope.GlAccountVat,
                    BillPayOption: $scope.BillPayOption,
                    IsPreRevenue: $scope.IsPreRevenue
                },
                dataType: "json",
                headers: { "Content-Type": "application/json" }
            }).then(function (response) {
                if (response.data === "Success") {
                    window.location.href = "/../ifmistz/Billings/PendingBills";
                } else {
                    alert(response.data);
                }
            });
        }
    };
});

myApp.controller("editController", function ($scope, $http, $filter) {

    $http({
        method: "GET",
        url: "/../ifmistz/Bills/GetCostings"
    }).then(function (response) {
        $scope.costings = response.data;
    });

    //$scope.editBill = function (id) {
    //    debugger
    //    $http({
    //        method: "get",
    //        url: "/../ifmistz/Bills/GetBill",
    //        params: { id: id }
    //    }).then(function (response) {
    //        $scope.BillId = 5;
    //        $scope.CostingId = response.data.CostingId;
    //        $scope.OperationalAmount = response.data.OperationalAmount;
    //        window.location.href = "/../ifmistz/Bills/EditBill";
    //    }, function () {
    //        alert("Error Occur");
    //    })
    //};

});