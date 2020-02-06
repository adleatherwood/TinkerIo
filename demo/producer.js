const util = require("./util")
const name = process.argv[2]

async function main() {
    for (i = 0; i < 100; i++) {
        const entry = { producer: name, message: (i + "").padStart(3, "0") }
        const result = await util.api("POST", "/stream/append/test2", entry)
        const output = JSON.stringify({ ...entry, result })

        console.log(output)
    }
}

main()