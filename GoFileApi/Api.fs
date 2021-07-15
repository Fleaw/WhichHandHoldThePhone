namespace GoFileApi

open FSharp.Data

module Api =

    type GetServerProvider = JsonProvider<"""https://api.gofile.io/getServer""">

    let GetServer =
        let getServerData = GetServerProvider.GetSample()
        getServerData.Data.Server