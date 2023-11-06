namespace OlxCrawler;

public static class ElementSelectors
{
    public static class HomePage
    {
        public const string SEARCH_BAR_INPUT = "search";
        public const string SUBMIT_BUTTON = "//*[@data-testid='search-submit']";
        public const string ACCEPT_COOKIES_BUTTON = "onetrust-accept-btn-handler";
        public const string FORWARD_BUTTON = "//*[@data-testid='pagination-forward']";
    }

    public static class OffersPage
    {
        public const string OFFER = "css-rc5s2u";
    }

    public static class OfferDetailsPage
    {
        public const string NUMBER_BUTTON_CLASS = "css-1nd8q08";
        public const string NUMBER_CLASS = "css-v1ndtc";
	}
}