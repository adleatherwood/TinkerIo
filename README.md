# TinkerIo

A simple file storage service providing basic key/value and stream-like storage directly to the filesystem.  This project is intended to be used in small projects where security among other features is not a concern.

## Getting Started

The following values are configurable by either command line or environment variable.

| CLI | ENV | DEFAULT | DESCRIPTION |
| - | - | - | - |
| --data | TINKERIO_DATA | ./data | The root location to persist data to
| --store | TINKERIO_STORE | ./data/stores | The root location to persist document stores
| --stream | TINKERIO_STREAM | ./data/streams | The root location to persist message streams to
| --writers | TINKERIO_WRITERS | 1000 | The number of file system writer actors to create
Example Usage

```bash
TINKERIO_STORE=~/data

./tinkerio --writers=500
```
## Document Storage & Cache Controllers

A simple key value store that writes each document to an individual file.  The Storage controller write documents to the configured storage location on disk.  The Cache controller is an in-memory store that has an identical API as the Storage controller.

### Method: Create

To create a document, no previous configuration is required.  Just provide the name of the store and the ID of the document in the URL.  Any name that is valid for the underlying file system can be used.

```json
PUT http://localhost:5000/store/create/myStore/myDocId

{
  "value": "1"
}

STATUS: 200

{
  "key": "myDocId",
  "hash": "a15ce1024b219fd76684ba1561d23ccc",
  "success": true,
  "message": "",
  "document": {
    "value": "1"
  }
}
```
If another document is created with the same id, an error is returned.

```json
PUT http://localhost:5000/store/create/myStore/myDocId

{
  "name": "philo"
}

STATUS: 200

{
  "key": "myDocId",
  "hash": "",
  "success": false,
  "message": "Document already exists",
  "document": {}
}
```
### Method: Read

To retrieve a document, provide the name of the store and an id to retrieve.

```json
GET http://localhost:5000/store/read/myStore/myDocId

STATUS: 200

{
  "key": "myDocId",
  "hash": "a15ce1024b219fd76684ba1561d23ccc",
  "success": true,
  "message": "",
  "document": {
    "value": "1"
  }
}
```
Attempting to retrieve a non-existing document will return the following:

```json
GET http://localhost:5000/store/read/myStore/doesntExist

STATUS: 200

{
  "key": "doesntExist",
  "hash": "",
  "success": false,
  "message": "Document does not exist",
  "document": {}
}
```
### Method: Replace

Use this method to overwrite an existing file with no restrictions.

```json
PUT http://localhost:5000/store/replace/myStore/myDocId

{
  "name": "philo"
}

STATUS: 200

{
  "key": "myDocId",
  "hash": "d7cf4ebd42f79bcaed487a77b9e1a3a8",
  "success": true,
  "message": "",
  "document": {
    "name": "philo"
  }
}
```
### Method: Update

Use this method to overwrite an existing file that has not been modified.  Every document is stored with a hash.  If your update contains the current hash, it will be successful.

```json
PUT http://localhost:5000/store/update/myStore/myDocId/d7cf4ebd42f79bcaed487a77b9e1a3a8

{
  "name": "nikos"
}

STATUS: 200

{
  "key": "myDocId",
  "hash": "758740134a1fbab8dd2c4abc4e48bb09",
  "success": true,
  "message": "",
  "document": {
    "name": "nikos"
  }
}
```
With an outdated hash, you will receive the following.

```json
PUT http://localhost:5000/store/update/myStore/myDocId/a-bad-hash-value

{
  "name": "nikos"
}

STATUS: 200

{
  "key": "myDocId",
  "hash": "",
  "success": false,
  "message": "Hash is out of date",
  "document": {}
}
```
### Method: Delete

Use the delete method to remove an existing file.

```json
DELETE http://localhost:5000/store/delete/myStore/myDocId

STATUS: 200

{
  "key": "myDocId",
  "hash": "d41d8cd98f00b204e9800998ecf8427e",
  "success": true,
  "message": "",
  "document": {}
}
```
Attempting to delete a non-existing document will also return successful:

```json
DELETE http://localhost:5000/store/delete/myStore/myDocId

STATUS: 200

{
  "key": "myDocId",
  "hash": "d41d8cd98f00b204e9800998ecf8427e",
  "success": true,
  "message": "",
  "document": {}
}
```
### Method: Publish

You can publish & subscribe to any document in the store.  In this way you can long-poll for changes and react to them as soon as they occur.

```json
PUT http://localhost:5000/store/publish/myStore/myDocId

{
  "data": "value"
}

STATUS: 200

{
  "key": "myDocId",
  "hash": "ab0243cd1e4cdd66d8a625822b744572",
  "success": true,
  "message": "",
  "document": {
    "data": "value"
  }
}
```
The behavior of the publish command is the same as performing a `create` or `replace`.  The is no check against the current hash to see if you are the last writer of the document.

### Method: Subscribe

When subscribing to a document in a store, you must supply the hash of the previous version of the document or an invalid hash value.  The request will be held for as long as possible until the document hash has changed.  It is up to the client to loop and reconnect in the case of a timeout.  In this manner, you get changes to your document as soon as they occur.

```json
GET http://localhost:5000/store/subscribe/myStore/myDocId/c20b1c60f2c69a03b9f687c96b902285

STATUS: 200

{
  "key": "myDocId",
  "hash": "ab0243cd1e4cdd66d8a625822b744572",
  "success": true,
  "message": "",
  "document": {
    "data": "value"
  }
}
```
It should be noted that publishing a document is the only way to notify subscribers of a change.  Simply replacing or updating the document will not trigger the change for subscribers.

## Message Stream Controller

A simple append-only stream that writes each document to an individual file.

### Method: Append

Specify the stream you would like to write to and the content of the message.  The stream will be created if it doesn't exist and an index number will be assigned to the written message

```json
POST http://localhost:5000/stream/append/myStream

{
  "nameSet": "philo"
}

STATUS: 200

{
  "stream": "myStream",
  "offset": 0
}
```
The offset is an unsigned int and will increment internally

```json
POST http://localhost:5000/stream/append/myStream

{
  "nameChanged": "kronos"
}

STATUS: 200

{
  "stream": "myStream",
  "offset": 1
}
```
### Method: Read

Use the read method to traverse the stream starting from any given offset.  The last segment of the URL is the number of documents to retrieve.

```json
GET http://localhost:5000/stream/read/myStream/0/2

STATUS: 200

{
  "stream": "myStream",
  "next": 2,
  "isEnd": true,
  "entries": [
    {
      "offset": 0,
      "isEnd": false,
      "error": null,
      "document": {
        "nameSet": "philo"
      }
    },
    {
      "offset": 1,
      "isEnd": true,
      "error": null,
      "document": {
        "nameChanged": "kronos"
      }
    }
  ]
}
```
You will be given the next offset to use in subsequent calls and an indicator that you have reached the end of the stream.

<div>Icons made by <a href="https://www.flaticon.com/authors/pixel-perfect" title="Pixel perfect">Pixel perfect</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a></div>

