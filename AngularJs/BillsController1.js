/// <reference path="angular.min.js" />

var myApp = angular.module("myModule", []);

myApp.controller("myController", function ($scope, $http) {

    $http({
        method: "GET",
        url: "/muse/ifmistz/Bills/GetCostings"
    }).then(function (response) {
        $scope.costings = response.data;
    });

    $http({
        method: "GET",
        url: "/muse/ifmistz/Bills/GetIdTypes"
    }).then(function (response) {
        $scope.idTypes = response.data;
    });

    var getCosting = function (id) {
        $http({
            method: "GET",
            url: "/muse/ifmistz/Bills/GetCosting",
            params: { id: id }
        }).then(function (response) {
            $scope.CostCode = response.data.CostCode,
            $scope.CostName = response.data.CostName,
            $scope.CostDesc = response.data.CostDesc,
            $scope.UOM = response.data.UOM,
            $scope.FinancialYearId = response.data.FinancialYearId,
            $scope.OperationalCurrency = response.data.OperationalCurrency,
            $scope.InstitutionId = response.data.InstitutionId,
            $scope.BillValidDays = response.data.BillValidDays,
            $scope.IsMainSource = response.data.IsMainSource,
            $scope.GLAccountReceivable = response.data.GLAccountReceivable,
            $scope.GLAccountRevenue = response.data.GLAccountRevenue,
            $scope.SubCostings = response.data.SubCostings
        });
    };
    $scope.getCosting = getCosting;

    var getSubCosting = function (id) {
        $http({
            method: "GET",
            url: "/muse/ifmistz/Bills/GetSubCosting",
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
            url: "/muse/ifmistz/Bills/GetCustomer",
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
            url: "/muse/ifmistz/Bills/GetIdType",
            params: { id: id }
        }).then(function (response) {
            $scope.IdType = response.data;
        });
    };
    $scope.getIdType = getIdType;

    var getOperationalAmount = function (para1, para2) {
        $scope.OperationalAmount = para1 * para2;
    };

    $scope.getOperationalAmount = getOperationalAmount;

    $scope.CreateBill = function () {
        debugger
        if (!$scope.form.$valid) {
            return false;
        }
        $http({
            method: "POST",
            url: "/muse/ifmistz/Bills/CreateBill",
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
                OperationalCurrency: $scope.OperationalCurrency,
                TxtCustomerName: $scope.CustomerName,
                DdlIdTypeId: $scope.IdTypeId
            },
            dataType: "json",
            headers: { "Content-Type": "application/json" }
        }).then(function (response) {
            if (response.data === "Success") {
                window.location.href = "/muse/ifmistz/Billings/PendingBills";
            } else {
                alert(response.data);
            }
        });
    };
});