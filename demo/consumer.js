const util = require("./util")

const name = process.argv[2]

async function main() {
    let next = 0

    while (next < 1000) {
        let result = await util.api("GET", "/topic/read/test2/" + next + "/10")

        next = result.next

        for (const entry of result.entries) {
            const output = JSON.stringify({ consumer: name, message: entry })
            console.log(output)
        }
    }

    console.log("CONSUMER: " + name + " EXITING...")
}

main()