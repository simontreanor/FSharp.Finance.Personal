namespace FSharp.Finance.B2B

/// Domain extensions for business product classification and metadata
module DomainExtensions =

    /// Business product types for categorization purposes only - does NOT determine regulatory status
    type BusinessProductType =
        /// Traditional consumer personal finance products (loans, credit cards, etc.)
        | Consumer
        /// Business-to-business financial products (trade credit, invoice factoring, etc.)
        | BusinessToBusiness
        /// Unspecified or mixed product type
        | Unspecified

    /// Product metadata for classification and analytical purposes only
    type ProductMetadata = {
        /// The business product type classification
        ProductType: BusinessProductType
        /// Optional descriptive name for the product
        Name: string option
        /// Optional additional categorization tags
        Tags: string array
    }

    /// Product metadata helper functions
    module ProductMetadata =
        
        /// Create consumer product metadata
        let consumer (name: string option) (tags: string array) =
            {
                ProductType = Consumer
                Name = name
                Tags = tags
            }

        /// Create business-to-business product metadata
        let businessToBusiness (name: string option) (tags: string array) =
            {
                ProductType = BusinessToBusiness
                Name = name
                Tags = tags
            }

        /// Create trade credit product metadata
        let tradeCredit (name: string option) =
            businessToBusiness name [| "trade-credit" |]

        /// Create invoice factoring product metadata
        let invoiceFactoring (name: string option) =
            businessToBusiness name [| "invoice-factoring" |]

        /// Create unspecified product metadata
        let unspecified (name: string option) (tags: string array) =
            {
                ProductType = Unspecified
                Name = name
                Tags = tags
            }

        /// Create default empty metadata
        let empty =
            {
                ProductType = Unspecified
                Name = None
                Tags = [||]
            }
