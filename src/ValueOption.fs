namespace FSharp.Finance.Personal

[<AutoOpen>]
module ValueOptionCE =

    type ValueOptionBuilder() = 

        member inline _.Bind(value: 'a voption, [<InlineIfLambda>] f: 'a -> 'b voption) : 'b voption = ValueOption.bind f value
        
        member inline _.Return(value: 'a) : 'a voption = ValueSome value

        member inline _.ReturnFrom(value: 'a voption) : 'a voption = value

        member inline this.Zero() = this.Return()

    let voption = ValueOptionBuilder()
    