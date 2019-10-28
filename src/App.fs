namespace TryElmish

module Router =
    open Browser.Types
    open Elmish.UrlParser

    type Model =
        | CreateRoute
        | ListRoute of string option
        | HomeRoute

    let init (model: Model option) =
        match model with
        | Some p -> p
        | None -> HomeRoute

    let toRoute route =
        match route with
        | CreateRoute -> "/item/create"
        | ListRoute q ->
            match q with
            | Some q -> sprintf "/item/list?q=%s" q
            | None -> "/item/list"
        | HomeRoute -> "/"

    let private route: Parser<Model -> Model, Model> =
        oneOf
            [ map CreateRoute (s "item" </> s "create")
              map ListRoute (s "item" </> s "list" <?> stringParam "q")
              map HomeRoute top ]

    let parser: Location -> Model option = parsePath route

module App =
    open Elmish
    open Elmish.Navigation
    open System

    type Model =
        { Notes: Map<string, string> }

    type Msg =
        | AddNote of string
        | Nav of Router.Model

    let init _ = { Notes = Map.empty }

    let private newId() = Guid.NewGuid().ToString()

    let update msg (model: Model) =
        match msg with
        | AddNote text ->
            { model with Notes = model.Notes.Add(newId(), text) },
            Navigation.newUrl (Router.toRoute (Router.ListRoute None))
        | Nav route -> model, Navigation.newUrl (Router.toRoute route)

module CreatePage =
    open Elmish
    open Elmish.Navigation
    open Fable.React
    open Fulma

    type Model =
        { Text: string }

    type Msg =
        | Save
        | SetText of string

    let init _ = { Text = "" }

    let update (msg: Msg) (model: Model) =
        match msg with
        | Save -> model, Navigation.newUrl "/item/list"
        | SetText text -> { model with Text = text }, Cmd.none

    let view (model: Model) dispatch dispatchOut =
        Section.section []
            [ h1 [] [ str "Add item" ]
              form []
                  [ Field.div []
                        [ Label.label [] [ str "Text" ]
                          Control.div []
                              [ Input.text
                                  [ Input.OnChange <| fun e ->
                                      e.preventDefault()
                                      e.Value
                                      |> SetText
                                      |> dispatch
                                    Input.Value model.Text ] ] ]
                    Field.div [ Field.IsGroupedRight ]
                        [ Control.div []
                              [ Button.button
                                  [ Button.OnClick <| fun e ->
                                      e.preventDefault()
                                      Router.ListRoute None
                                      |> App.Nav
                                      |> dispatchOut
                                    Button.Color IsLink ] [ str "Cancel" ] ]
                          Control.div []
                              [ Button.button
                                  [ Button.OnClick <| fun e ->
                                      e.preventDefault()
                                      model.Text
                                      |> App.AddNote
                                      |> dispatchOut
                                    Button.Color IsPrimary ] [ str "Save" ] ] ] ] ]

module HomePage =
    open Fable.React
    open Fable.React.Props
    open Fulma

    let view _ _ dispatchOut =
        Section.section []
            [ h1 [] [ str "Trying Elmish" ]
              p [] [ str "It's pretty basic, but the code is hopefully nice." ]
              Card.card [ Props [ Style [ MarginBottom "2rem" ] ] ]
                  [ Card.header [] [ Card.Header.title [] [ str "Items" ] ]
                    Card.content []
                        [ p []
                              [ str
                                  "Here you can access the main functionality of this application: adding and viewing items!" ]
                          Content.content [ Content.Size IsSmall ] [ p [] [ str "...no delete" ] ] ]
                    Card.footer []
                        [ Card.Footer.a
                            [ Props [ Href "/item/create" ]
                              Props
                                  [ OnClick <| fun e ->
                                      e.preventDefault()
                                      Router.CreateRoute
                                      |> App.Nav
                                      |> dispatchOut ] ] [ str "Add item" ]
                          Card.Footer.a
                              [ Props [ Href "/item/list" ]
                                Props
                                    [ OnClick <| fun e ->
                                        e.preventDefault()
                                        Router.ListRoute None
                                        |> App.Nav
                                        |> dispatchOut ] ] [ str "View items" ] ] ] ]

module ListPage =
    open Fable.FontAwesome
    open Fable.React
    open Fable.React.Props
    open Fulma

    let view (appModel: App.Model) _ _ dispatchOut =
        Section.section []
            [ h1 [] [ str "Items" ]
              Breadcrumb.breadcrumb []
                  [ Breadcrumb.item []
                        [ a
                            [ Href(Router.toRoute Router.HomeRoute)
                              OnClick <| fun e ->
                                  e.preventDefault()
                                  App.Nav Router.HomeRoute |> dispatchOut ]
                              [ Icon.icon [ Icon.Size IsSmall ] [ Fa.i [ Fa.Solid.Home ] [] ]
                                str "Home" ] ] ]
              for KeyValue(k, v) in appModel.Notes do
                  p [] [ str v ] ]

module Main =
    open Elmish
    open Elmish.Navigation
    open Elmish.React
    open Fulma

    type Model =
        { AppModel: App.Model
          CreateModel: CreatePage.Model
          RouteModel: Router.Model }

    type Msg =
        | AppMsg of App.Msg
        | CreateMsg of CreatePage.Msg

    let init _ =
        { AppModel = App.init()
          CreateModel = CreatePage.init()
          RouteModel = Router.init None }, Cmd.none

    let update (msg: Msg) (model: Model) =
        match msg with
        | AppMsg msg' ->
            let field, cmd = App.update msg' model.AppModel
            { model with AppModel = field }, cmd
        | CreateMsg msg' ->
            let field, cmd = CreatePage.update msg' model.CreateModel
            { model with CreateModel = field }, cmd

    let urlUpdate (result: Option<Router.Model>) (model: Model): Model * Cmd<Msg> =
        match result with
        | Some r -> { model with RouteModel = r }, Cmd.none
        | None -> model, Cmd.none

    let viewRouteContent model dispatch =
        let dispatchOut = AppMsg >> dispatch

        match model.RouteModel with
        | Router.CreateRoute -> CreatePage.view model.CreateModel (CreateMsg >> dispatch) dispatchOut
        | Router.HomeRoute -> HomePage.view () () dispatchOut
        | Router.ListRoute _ -> ListPage.view model.AppModel () () dispatchOut

    let view model dispatch = Content.content [] [ viewRouteContent model dispatch ]

    // open Elmish.Debug
    Program.mkProgram init update view
    |> Program.toNavigable Router.parser urlUpdate
    |> Program.withConsoleTrace
    |> Program.withReactSynchronous "elmish-app"
    |> Program.run
