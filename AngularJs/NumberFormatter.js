(function (angular) {
    "use strict";

    //function mainCtrl() {
    //    var vm = this;
    //    vm.numericValue = 12345678;
    //}

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
                    if (decimalPlaces != null && decimalPlaces.length > fractionSize) {
                        //Trim extra decimal places
                        decimalPlaces = decimalPlaces.substring(0, fractionSize);
                        trimmedValue = arr[0] + separator + decimalPlaces;
                    }
                    var numericValue = parseFloat(trimmedValue);
                    var isEmpty = numericValue == null || viewValue.trim() === "";
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

    //angular
    //    .module("app", [])
    //    .controller("mainCtrl", mainCtrl)
    //    .directive("sgNumberInput", sgNumberInput);
})(angular);