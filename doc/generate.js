const http = require('http')
const fs = require('fs');
const Path = require('path');

function writeLn(text) {
    if (!text) text = ""
    fs.appendFileSync ("../README.md", text + "\r\n")
}

const deleteFolderRecursive = function(path) {
  if (fs.existsSync(path)) {
    fs.readdirSync(path).forEach((file, index) => {
      const curPath = Path.join(path, file);
      if (fs.lstatSync(curPath).isDirectory()) { // recurse
        deleteFolderRecursive(curPath);
      } else { // delete file
        fs.unlinkSync(curPath);
      }
    });
    fs.rmdirSync(path);
  }
}

function api(method, path, content) {
    return new Promise((resolve, reject) => {
        var json = JSON.stringify(content, null, 2)
        var options = {
            host: 'localhost',
            port: 5000,
            path: path,
            method: method,
            headers: content
                ?
                    {
                    'Content-Type': 'application/json',
                    'Content-Length': Buffer.byteLength(json)
                    }
                : null
        };

        writeLn('```json');
        writeLn(method + " http://localhost:5000" + path);
        if (content) {
            writeLn();
            writeLn(json)
        }
        writeLn();

        var req = http.request(options, function(res) {
            writeLn('STATUS: ' + res.statusCode)
            writeLn()
            //console.log('HEADERS: ' + JSON.stringify(res.headers));
            res.setEncoding('utf8');
            res.on('data', function (chunk) {
                let object = JSON.parse(chunk)
                let json = JSON.stringify(object, null, 2)
                //console.log('```json');
                writeLn(json);
                writeLn('```');
                resolve(object)
            });
        });

        req.on('error', function(e) {
            console.log('problem with request: ' + e.message);
        });

        if (content) {
            req.write(json);
        }

        req.end();
    })
}

let output = ""

function h1(text) {
    writeLn("# " + text)
    writeLn()
}

function h2(text) {
    writeLn("## " + text)
    writeLn()
}

function h3(text) {
    writeLn("### " + text)
    writeLn()
}

function p(text) {
    writeLn(text)
    writeLn()
}

function l(text) {
    writeLn(text)
}

async function main() {
    fs.unlinkSync("../README.md")
    deleteFolderRecursive("../src/data")

    h1("TinkerIo")

    p("A simple file storage service providing basic key/value and stream-like storage directly to the filesystem.  This project is intended to be used in small projects where security among other features is not a concern.")

    h2("Getting Started")

    p("The following values are configurable by either command line or environment variable.")

    l("| CLI | ENV | DEFAULT | DESCRIPTION |")
    l("| - | - | - | - |")
    l("| --data | TINKERIO_DATA | ./data | The root location to persist data to")
    l("| --store | TINKERIO_STORE | ./data/stores | The root location to persist document stores")
    l("| --stream | TINKERIO_STREAM | ./data/streams | The root location to persist message streams to")
    l("| --writers | TINKERIO_WRITERS | 1000 | The number of file system writer actors to create")

    p("Example Usage")
    l("```bash")
    l("TINKERIO_STORE=~/data")
    l("")
    l("./tinkerio --writers=500")
    l("```")

    h2("Document Storage Controller")

    p("A simple key value store that writes each document to an individual file.")

    h3("Method: Create")

    p("To create a document, no previous configuration is required.  Just provide the name of the store and the ID of the document in the URL.  " +
      "Any name that is valid for the underlying file system can be used.")

    await api("PUT", "/store/create/myStore/myDocId", {"value": "1"})

    p("If another document is created with the same id, an error is returned.")

    await api("PUT", "/store/create/myStore/myDocId", {"name": "philo"})

    h3("Method: Read")

    p("To retrieve a document, provide the name of the store and an id to retrieve.")

    await api("GET", "/store/read/myStore/myDocId")

    p("Attempting to retrieve a non-existing document will return the following:")

    await api("GET", "/store/read/myStore/doesntExist")

    h3("Method: Replace")

    p("Use this method to overwrite an existing file with no restrictions.")

    let a = await api("PUT", "/store/replace/myStore/myDocId", {"name": "philo"})

    h3("Method: Update")

    p("Use this method to overwrite an existing file that has not been modified.  " +
      "Every document is stored with a hash.  If your update contains the current hash, it will be successful.")

    await api("PUT", "/store/update/myStore/myDocId/" + a.hash, {"name": "nikos"})

    p("With an outdated hash, you will receive the following.")

    await api("PUT", "/store/update/myStore/myDocId/a-bad-hash-value", {"name": "nikos"})

    h3("Method: Delete")

    p("Use the delete method to remove an existing file.")

    await api("DELETE", "/store/delete/myStore/myDocId")

    p("Attempting to delete a non-existing document will also return successful:")

    await api("DELETE", "/store/delete/myStore/myDocId")

    h3("Method: Publish")

    p("You can publish & subscribe to any document in the store.  In this way you can long-poll for changes and " +
      "react to them as soon as they occur.")

    await api("PUT", "/store/publish/myStore/myDocId", { "data": "value" })

    p("The behavior of the publish command is the same as performing a `create` or `replace`.  The is no check against the current hash " +
      "to see if you are the last writer of the document.")

    h3("Method: Subscribe")

    p("When subscribing to a document in a store, you must supply the hash of the previous version of the document or an invalid hash value.  " +
      "The request will be held for as long as possible until the document hash has changed.  It is up to the client to loop and reconnect in " +
      "the case of a timeout.  In this manner, you get changes to your document as soon as they occur.")

    await api("GET", "/store/subscribe/myStore/myDocId/c20b1c60f2c69a03b9f687c96b902285")

    p("It should be noted that publishing a document is the only way to notify subscribers of a change.  Simply replacing or updating the " +
      "document will not trigger the change for subscribers.")

    h2("Message Stream Controller")

    p("A simple append-only stream that writes each document to an individual file.")

    h3("Method: Append")

    p("Specify the stream you would like to write to and the content of the message.  " +
      "The stream will be created if it doesn't exist and an index number will be assigned to the written message")

    await api("POST", "/stream/append/myStream", { "nameSet": "philo"})

    p("The offset is an unsigned int and will increment internally")

    await api("POST", "/stream/append/myStream", { "nameChanged": "kronos"})

    h3("Method: Read")

    p("Use the read method to traverse the stream starting from any given offset.  " +
      "The last segment of the URL is the number of documents to retrieve.")

    await api("GET", "/stream/read/myStream/0/2")

    p("You will be given the next offset to use in subsequent calls and an indicator that you have reached the end of the stream.")

    p("<div>Icons made by <a href=\"https://www.flaticon.com/authors/pixel-perfect\" title=\"Pixel perfect\">Pixel perfect</a> from <a href=\"https://www.flaticon.com/\" title=\"Flaticon\">www.flaticon.com</a></div>")
}

main()