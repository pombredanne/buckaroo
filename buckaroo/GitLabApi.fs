module Buckaroo.GitLabApi

open System
open FSharp.Data

let fetchFile (package : AdhocPackageIdentifier) (commit : Revision) (file : string) = async {
  if commit.Length <> 40
  then
    return raise <| new ArgumentException("GitLab API requires full length commit hashes")
  else
    let url = 
      "https://gitlab.com/" + package.Owner + "/" + package.Project + "/raw/" + commit + "/" + file
    return! Http.AsyncRequestString(url)
}
