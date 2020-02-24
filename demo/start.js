const childProcess = require("child_process");
const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

for (i = 0; i < 26; i++) {
    childProcess.fork("producer.js", [alphabet[i], "topic", 10])
    childProcess.fork("consumer.js", [alphabet[i], "topic", 26 * 10])
}
