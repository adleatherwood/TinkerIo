const childProcess = require("child_process");
const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

childProcess.fork("view.js", [26 * 10])

for (i = 0; i < 26; i++) {
    childProcess.fork("producer.js", [alphabet[i], "stream", 10])
    childProcess.fork("consumer.js", [alphabet[i], "stream", 26 * 10])
}
