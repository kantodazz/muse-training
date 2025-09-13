/**
* Author: Jeraldy Matara Deus | deusjeraldy@gmail.com
* Reusable Custom HTMLElements
*/
class AmountInput extends HTMLElement {

    constructor() {
        super();
        this.addEventListener('keyup', this._amountChanged.bind(this));
        this.oldVal = 0;
    }

    connectedCallback() {
        this.innerHTML = this._rederCustomElement();
    }

    set setVal(val) {
        val.event.target.value = val.value == undefined ? 0 : val.value;
    }

    _rederCustomElement() {
        let propsObj = {};
        this.getAttributeNames().forEach(attr=> {
            propsObj[attr] = this.getAttribute(attr)
        })
        let props = ""
        if (Object.keys(propsObj).length > 0) {
            props = JSON.stringify(propsObj);
            props = props.split("}").join(" ").split('{"').join(" ")
            props = props.split('":').join("=")
            props = props.split(',"').join(" ")
            props = props.split('id="').join('id="_')
        }
        return "<input type='text' " + props + " />"
    }

    _amountChanged(event) {
        this.setVal = { event: event, value: this._addSeparator(event.target.value) }
    }

    _toLabel(number) {
        return parseFloat(number).toLocaleString()
    }

    _validateDots(val) {
        if (isNaN(val)) {
            val = val.replace(/[^0-9\.]/g, '');
            if (val.split('.').length > 2)
                val = val.replace(/\.+$/, "");
        }
        return val
    }

    _addSeparator(newVal) {
        let counter = 0
        if (newVal) {
            try {
                if (newVal.endsWith(".")) {
                    return this._validateDots(newVal)
                }
                if (newVal.endsWith(".0")) {
                    return this._toLabel(this._validateDots(newVal)) + ".0"
                }
                if (newVal.endsWith(".00")) {
                    return this._toLabel(this._validateDots(newVal)) + ".00"
                }
                newVal = parseFloat(newVal.split(",").join(""))
                if (typeof newVal == 'number' && !Number.isNaN(newVal)) {
                    this.oldVal = newVal;
                    this.value = newVal;
                    return newVal.toLocaleString()
                } else {
                    this.value = this.oldVal;
                    return this.oldVal
                }
            } catch (e) {
                console.log(e)
            }
        }
    }
}

customElements.define('amount-input', AmountInput);

class PercentInput extends HTMLElement {

    constructor() {
        super();
        this.addEventListener('keyup', this._amountChanged.bind(this));
        this.oldVal = 0;
    }

    connectedCallback() {
        this.innerHTML = this._rederCustomElement();
    }

    set setVal(val) {
        val.event.target.value = val.value == undefined ? 0 : val.value;
    }

    _rederCustomElement() {
        let propsObj = {};
        this.getAttributeNames().forEach(attr=> {
            propsObj[attr] = this.getAttribute(attr)
        })
        let props = ""
        if (Object.keys(propsObj).length > 0) {
            props = JSON.stringify(propsObj);
            props = props.split("}").join(" ").split('{"').join(" ")
            props = props.split('":').join("=")
            props = props.split(',"').join(" ")
            props = props.split('id="').join('id="_')
        }
        return "<input type='text' " + props + " />"
    }

    _amountChanged(event) {
        this.setVal = { event: event, value: this._addSeparator(event.target.value) }
    }

    _toLabel(number) {
        return parseFloat(number).toLocaleString()
    }

    _validateDots(val) {
        if (isNaN(val)) {
            val = val.replace(/[^0-9\.]/g, '');
            if (val.split('.').length > 2)
                val = val.replace(/\.+$/, "");
        }
        return val
   }

    _addSeparator(newVal) {
        let counter = 0
        if (newVal) {
            try {
                if (newVal.endsWith(".")) {
                    return this._validateDots(newVal)
                }
                if (newVal.endsWith(".0")) {
                    return this._toLabel(this._validateDots(newVal)) + ".0"
                }
                if (newVal.endsWith(".00")) {
                    return this._toLabel(this._validateDots(newVal)) + ".00"
                }
                newVal = parseFloat(newVal.split(",").join(""))
                if (typeof newVal == 'number' && !Number.isNaN(newVal)) {
                    if (parseFloat(newVal) > 100) {
                        this.value = this.oldVal;
                        return this.oldVal
                    } else {
                        this.oldVal = newVal;
                        this.value = newVal;
                        return newVal.toLocaleString()
                    }
                } else {
                    this.value = this.oldVal;
                    return this.oldVal
                }
            } catch (e) {
                console.log(e)
            }
        }
    }
}

customElements.define('percent-input', PercentInput);
