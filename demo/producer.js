const client = require("./client")
const calc = require("./calc")

async function send(kind, document) {
    let result = null

    if (kind === "stream") {
        result = await client.stream.append("demo", document)
    }
    else if (kind === "topic") {
        result = await client.topic.append("demo", document)
    }

    return result
}

async function main(name, kind, count) {
    for (var i = 0; i < count; i++) {
        const document = calc.random()
        const result = await send(kind, document)

        result.document = document

        //console.log("PRODUCER: " + name + "; SENT: " + JSON.stringify(result))
    }
}

const name = process.argv[2]
const kind = process.argv[3]
const count = process.argv[4]

main(name, kind, count)
