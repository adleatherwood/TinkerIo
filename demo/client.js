const util = require("./util")

function source(targetApi) {
    return {
        targetApi: targetApi,

        append: async function (sourceName, entry) {
            const result = await util.api("POST", "/" + this.targetApi + "/append/" + sourceName, entry)
            return result
        },
        consume: async function (sourceName, batchSize, onEntry) {
            let next = 0
            let quit = false

            while (!quit) {
                let result = await util.api("GET", "/" + this.targetApi + "/read/" + sourceName + "/" + next + "/" + batchSize)

                next = result.next

                for (const entry of result.entries) {
                    quit = await onEntry(entry)
                    if (quit)
                        break;
                }
            }
        }
    }
}

const stream = source("stream")
const topic = source("topic")

function crud(targetApi) {
    return {
        targetApi: targetApi,

        create: async function (storeName, documentId, document) {
            const result = await util.api("PUT", "/" + this.targetApi + "/create/" + storeName + "/" + documentId, document)
            return result
        },
        read: async function (storeName, documentId) {
            const result = await util.api("GET", "/" + this.targetApi + "/read/" + storeName + "/" + documentId)
            return result
        },
        replace: async function (storeName, documentId, document) {
            const result = await util.api("PUT", "/" + this.targetApi + "/replace/" + storeName + "/" + documentId, document)
            return result
        },
        update: async function (storeName, documentId, hash, document) {
            const result = await util.api("PUT", "/"+ this.targetApi +"/update/"+ storeName +"/"+ documentId +"/" + hash, document)
            return result
        },
        delete: async function (storeName, documentId) {
            const result = await util.api("DELETE", "/"+ this.targetApi +"/delete/"+ storeName +"/" + documentId)
            return result
        },
        publish: async function (storeName, documentId, document) {
            const result = await util.api("PUT", "/"+ this.targetApi +"/publish/"+ storeName +"/" + documentId, document)
            return result
        },
        subscribe: async function (storeName, documentId, onDocument) {
            let quit = false
            while (!quit) {
                const result = await util.api("GET", "/"+ this.targetApi +"/subscribe/"+ storeName +"/"+ documentId +"/" + hash)
                quit = await onDocument(result)
            }
        }
    }
}

const store = crud("store")
const cache = crud("cache")

module.exports = {
    store: store,
    cache: cache,
    stream: stream,
    topic: topic
}
