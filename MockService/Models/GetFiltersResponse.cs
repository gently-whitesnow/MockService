namespace MockService.Models
{
    public class GetFiltersResponse
    {
        /// <summary>
        /// Название фильтра
        /// </summary>
        public string FilterName { get; set; }
        
        /// <summary>
        /// Количество моков
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// Дата последнего добавленного мока
        /// </summary>
        public DateTime LastCreateDate { get; set; }
    }
}