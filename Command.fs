module Command

open System
open FParsec

open Lock

type Dependency = Dependency.Dependency
type Manifest = Manifest.Manifest

type Command = 
| ListDependencies
| Resolve
| Install
| AddDependencies of List<Dependency>

let listDependenciesParser = parse {
  do! CharParsers.spaces
  do! CharParsers.skipString "list"
  do! CharParsers.spaces
  return ListDependencies
}

let resolveParser = parse {
  do! CharParsers.spaces
  do! CharParsers.skipString "resolve"
  do! CharParsers.spaces
  return Resolve
}

let installParser = parse {
  do! CharParsers.spaces
  do! CharParsers.skipString "install"
  do! CharParsers.spaces
  return Install
}

let addDependenciesParser = parse {
  do! CharParsers.spaces
  do! CharParsers.skipString "add"
  do! CharParsers.spaces1
  let! deps = Primitives.sepEndBy1 Dependency.parser CharParsers.spaces1
  return AddDependencies deps
}

let parser = 
  listDependenciesParser 
  <|> resolveParser
  <|> addDependenciesParser
  <|> installParser

let parse x = 
  match run parser x with
  | Success(result, _, _) -> Result.Ok result
  | Failure(error, _, _) -> Result.Error error

let readFile (path : string) = async {
  use sr = new IO.StreamReader(path)
  return! sr.ReadToEndAsync() |> Async.AwaitTask
}

let writeFile (path : string) (content : string) = async {
  use sw = new IO.StreamWriter(path)
  return! sw.WriteAsync(content) |> Async.AwaitTask
}

let readManifest = async {
  let! content = readFile "buckaroo.txt"
  return 
    match Manifest.parse content with 
    | Result.Ok manifest -> manifest
    | Result.Error error -> new Exception("Error parsing manifest:\n" + error) |> raise
}

let writeManifest (manifest : Manifest) = async {
  let content = Manifest.show manifest
  return! writeFile "buckaroo.txt" content
}

let readLock = async {
  let! content = readFile "buckaroo.lock.txt"
  return { Packages = set [] } // TODO
}

let listDependencies = async {
  let! manifest = readManifest
  manifest.Dependencies
  |> Seq.distinct
  |> Seq.map Dependency.show
  |> String.concat "\n"
  |> Console.WriteLine
  return ()
}

let add dependencies = async {
  let! manifest = readManifest
  let newManifest = { 
    manifest with 
      Dependencies = 
        manifest.Dependencies 
        |> Seq.append dependencies 
        |> Set.ofSeq;
  }
  if manifest = newManifest 
  then return ()
  else 
    let! resolution = Solver.solve newManifest
    do! writeManifest newManifest
    return ()
}

let resolve = async {
  let! manifest = readManifest
  let! resolution = Solver.solve manifest
  Console.WriteLine (Solver.show resolution)
  return ()
}

let install = async {
  let! lock = readLock
  return ()
}

let runCommand command = 
  match command with
  | ListDependencies -> listDependencies
  | Resolve -> resolve
  | Install -> install
  | AddDependencies dependencies -> add dependencies