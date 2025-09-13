// The validateage function
$.validator.addMethod(
    'requiredif',
    function (value, element, params) {
        return value;
    });

$.validator.unobtrusive.adapters.add(
    'requiredif', ['dependentProperty', 'targetValue'], function (options) {
        var params = {
            dependentProperty: options.params.dependentProperty,
            targetValue: options.params.targetValue
        };
        options.rules['requiredif'] = params;
        options.messages['requiredif'] = options.message;
    });