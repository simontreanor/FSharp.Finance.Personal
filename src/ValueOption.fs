namespace FSharp.Finance.Personal

/// a computation expression enabling easier handling of functions that return value options
module ValueOptionCE =

    type ValueOptionBuilder() = 

        member inline _.Bind(value: 'a voption, [<InlineIfLambda>] f: 'a -> 'b voption) : 'b voption = ValueOption.bind f value
        
        member inline _.Return(value: 'a) : 'a voption = ValueSome value

        member inline _.ReturnFrom(value: 'a voption) : 'a voption = value

        member inline this.Zero() = this.Return()

    let voption = ValueOptionBuilder()
    
    /// convert an option to a value option
    let toValueOption = function Some x -> ValueSome x | None -> ValueNone
