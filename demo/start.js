const util = require("./util")
const childProcess = require("child_process");
const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

//util.deleteFolderRecursive ("../src/data")

// spawn n producers & consumers

for (i = 0; i < 10; i++) {
    childProcess.fork("producer.js", [alphabet[i]])
    childProcess.fork("consumer.js", [alphabet[i]])
}
