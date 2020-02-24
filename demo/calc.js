const util = require("./util")

function makeEvent(action, amount) {
    return {
        "action": action,
        "amount": amount
    }
}

function add(amount) {
    return makeEvent("add", amount)
}

function random() {
    const amount = util.random(1, 10)
    return add(amount)
}

module.exports = {
    random: random
}