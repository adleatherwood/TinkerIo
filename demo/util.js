const http = require('http')
const fs = require('fs');
const Path = require('path');

function random(min, max) {
    min = Math.ceil(min)
    max = Math.floor(max + 1)
    return Math.floor(Math.random() * (max - min)) + min
}

function deleteFolder(path) {
    if (fs.existsSync(path)) {
        fs.readdirSync(path).forEach((file, index) => {
            const curPath = Path.join(path, file);
            if (fs.lstatSync(curPath).isDirectory()) {
                deleteFolder(curPath);
            } else {
                fs.unlinkSync(curPath);
            }
        });
        fs.rmdirSync(path);
    }
}

function api(method, path, content) {
    let chunks = []
    return new Promise((resolve, reject) => {
        var json = JSON.stringify(content, null, 2)
        // console.log(method)
        // console.log(path)
        // console.log(json)
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

        var req = http.request(options, function(res) {
            res.setEncoding('utf8');
            res.on('data', function (chunk) {
                chunks.push(chunk)
            });
            res.on('end', function() {
                let object = JSON.parse(chunks)

                resolve(object)
            })
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

module.exports = {
    random: random,
    deleteFolder: deleteFolder,
    api: api
}