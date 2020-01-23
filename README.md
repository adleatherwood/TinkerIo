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
## Document Storage Controller

A simple key value store that writes each document to an individual file.

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
  "message": "File already exists",
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
  "message": "File does not exist",
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
## Message Stream Controller

A simple append-only stream that writes each document to an individual file.

### Method: Append

Simply specify the stream you would like to write to and the content of the message.  The stream will be created if it doesn't exist and an index number will be assigned to the written message

```json
POST http://localhost:5000/stream/append/myStream

{
  "nameSet": "philo"
}

STATUS: 200

{
  "offset": 0,
  "success": true,
  "message": ""
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
  "offset": 1,
  "success": true,
  "message": ""
}
```
### Method: Read

Use the read method to traverse the stream starting from any given offset.  The last segment of the URL is the number of documents to retrieve.

```json
GET http://localhost:5000/stream/read/myStream/0/2

STATUS: 200

{
  "entries": [
    {
      "offset": 0,
      "document": {
        "nameSet": "philo"
      },
      "isEnd": false
    },
    {
      "offset": 1,
      "document": {
        "nameChanged": "kronos"
      },
      "isEnd": true
    }
  ],
  "next": 2,
  "isEnd": true
}
```
You will be given the next offset to use in subsequent calls and an indicator that you have reached the end of the stream.

<div>Icons made by <a href="https://www.flaticon.com/authors/freepik" title="Freepik">Freepik</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a></div>
