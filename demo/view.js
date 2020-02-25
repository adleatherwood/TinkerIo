const client = require("./client")
const count = process.argv[2]

async function onEntry(entry) {
    console.log(entry.document)

    if (entry.document.count >= count )
        return true
}

async function main() {
    client.cache.subscribe("demo", "total", onEntry)
}

main()