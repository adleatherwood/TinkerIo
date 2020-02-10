namespace TinkerIo.Cache

open TinkerIo

module Cache =

    let post (request: CrudRequest) = async {
        let! response =
            match request with
            | Create  c -> Crud.create  DictIo.Services c
            | Read    r -> Crud.read    DictIo.Services r
            | Replace r -> Crud.replace DictIo.Services r
            | Update  u -> Crud.update  DictIo.Services u
            | Delete  d -> Crud.delete  DictIo.Services d
            | Publish p -> Crud.publish DictIo.Services p
        return response
    }

