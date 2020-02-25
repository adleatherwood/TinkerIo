const client = require("./client")
const name = process.argv[2]
const kind = process.argv[3]
const count = process.argv[4]

let total = 0
let i = 1;

async function onEntry(entry) {
    //console.log("CONSUMER: " + name + "; RECV: " + JSON.stringify(entry))

    switch (entry.document.action) {
        case "add": { total += entry.document.amount }
    }

    i++

    client.cache.publish("demo", "total", { "total": total, "count": i })

    return i >= count
}

async function main(name, kind) {
    if (kind === "stream") {
        await client.stream.consume("demo", 10, onEntry)
    }
    else if (kind === "topic") {
        await client.topic.consume("demo", 10, onEntry)
    }

    setTimeout(() =>
        console.log("CONSUMER: " + name + "; TOTAL: " + total),
        500);
}

main(name, kind)