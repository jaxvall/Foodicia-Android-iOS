﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CaptoApplication
{

    public class RecipesScraper
    {

        int IndexOfSecond(string theString, string toFind)
        {
            int first = theString.IndexOf(toFind);

            if (first == -1) return -1;

            // Find the "next" occurrence by starting just past the first
            return theString.IndexOf(toFind, first + 1);
        }

        public string Url { get; set; }

        public HttpClient httpclient { get; set; }

        public string Searchword { get; set; }

        public int? NumPages { get; set; }

        public List<Recipe> ListOfRecipes { get; set; }

        public RecipesScraper()
        {
            ListOfRecipes = new List<Recipe>();

        }

        public async Task<List<Recipe>> GetRecipesCoop(string searchword)
        {
            try
            {
                string search = "https://www.coop.se/globalt-sok/?query=" + searchword;
                
                httpclient = new HttpClient();
                var html = await httpclient.GetStringAsync(search);

                var htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(html);

                var receptLista = new List<HtmlNode>();
                receptLista = htmldoc.DocumentNode.Descendants("div")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("Grid Grid--recipe Grid--gutterAxsm js-recipesSearchResultList u-lg-marginTz")).ToList();

                var finalList = new List<HtmlNode>();
                finalList = receptLista[0].Descendants("article").ToList();

                if (finalList.Count > 9)
                {
                    int limit = finalList.Count - 9;
                    finalList.RemoveRange(8, limit);
                }

                foreach (var item in finalList)
                {
          
                    string url = "https://www.coop.se" + item.SelectSingleNode("div[1]/a").GetAttributeValue("href", "");

                    using (HttpClient client = new HttpClient())
                    {

                        var html2 = await client.GetStringAsync(url);
                        HtmlDocument htmldoc2 = new HtmlDocument();
                        htmldoc2.LoadHtml(html2);

                        var root = new List<HtmlNode>();
                        root = htmldoc2.DocumentNode.Descendants("div")
                        .Where(node => node.GetAttributeValue("class", "")
                        .Equals("u-lg-hidden")).ToList();

                        //getImage
                       
                        string image = "https:" + root[0].SelectSingleNode("div[1]/img").GetAttributeValue("src", "");

                        //getTitle

                        string title = root[0].SelectSingleNode("div[2]/div[1]/h1").InnerHtml;
                        title = convertUTF(title);

                        //getDescription

                        string description = root[0].SelectSingleNode("div[2]/div[1]/p").InnerHtml;
                        description = convertUTF(description);

                        description = limitDescription(title, description);

                        title = limitTitle(title);

                        //GetIngredienserToList

                        var ingredientRoot = root[0].Descendants("li")
                                                    .Where(node => node.GetAttributeValue("class", "")
                                                    .Equals("u-paddingHxsm u-textNormal u-colorBase")).ToList();

                        var ingredientList = new List<Ingredient>();

                        foreach (HtmlNode nod in ingredientRoot)
                        {
                            HtmlNode node = nod.SelectSingleNode("span[@class='u-textWeightBold ']");
                            string ingredientString = convertUTF(node.InnerHtml);
                            ingredientList.Add(new Ingredient(ingredientString));
                        }
              
                        if (ingredientList.Count != 0)
                        {
                            var recipe = new Recipe(title, description, ingredientList, url, ingredientList.Count, image);

                            ListOfRecipes.Add(recipe);
                            SetRecipeMatches(recipe);
                        }
                            
                    }
                }

                return ListOfRecipes;
            }
            catch(Exception)
            {
                return new List<Recipe>();
            }

        }

        public async Task<List<Recipe>> GetRecipesTasteline(string searchword, string currentPage)
        {
            try
            {
                string search = "https://www.tasteline.com/sok/" + searchword + "/sida/" + currentPage;

                httpclient = new HttpClient();
                var html = await httpclient.GetStringAsync(search);

                var htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(html);

                var receptLista = new List<HtmlNode>();
                receptLista = htmldoc.DocumentNode.Descendants("a")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("list-item__link")).ToList();

                foreach (var item in receptLista)
                {
                    string url = item.GetAttributeValue("href", "");

                    using (HttpClient client = new HttpClient())
                    {

                        var html2 = await client.GetStringAsync(url);
                        HtmlDocument htmldoc2 = new HtmlDocument();
                        htmldoc2.LoadHtml(html2);

                        var root = new List<HtmlNode>();
                        root = htmldoc2.DocumentNode.Descendants("div")
                        .Where(node => node.GetAttributeValue("class", "")
                        .Equals("container page-content clearfix")).ToList();

                        //getImage

                        string image = root[0].SelectSingleNode("div[2]/div[1]/div[1]/div[1]/img").GetAttributeValue("src", "");

                        //getTitle

                        string title = root[0].SelectSingleNode("div[2]/div[1]/div[1]/div[2]/h1").InnerHtml;
                        title = convertUTF(title);

                        //getDescription

                        string description = root[0].SelectSingleNode("div[2]/div[1]/div[1]/div[2]/div[1]").InnerHtml;
                        description = convertUTF(description);

                        description = limitDescription(title, description);

                        title = limitTitle(title);

                        //GetIngredienserToList

                        var ingredientRoot = root[0].Descendants("a")
                                                    .Where(node => node.GetAttributeValue("class", "")
                                                    .Equals("ingredient")).ToList();

                        var ingredientList = new List<Ingredient>();

                        foreach (HtmlNode nod in ingredientRoot)
                        {
                            HtmlNode node = nod.SelectSingleNode("span");
                            string ingredientString = convertUTF(node.InnerHtml);
                            ingredientList.Add(new Ingredient(ingredientString));
                        }

                        if (ingredientList.Count != 0)
                        {
                            var recipe = new Recipe(title, description, ingredientList, url, ingredientList.Count, image);

                            ListOfRecipes.Add(recipe);
                            SetRecipeMatches(recipe);
                        }
                            
                    }
                }

                return ListOfRecipes;
            }
            catch (Exception)
            {
                return new List<Recipe>();
            }
        }

        public async Task<List<Recipe>> GetRecipesMittkok(string searchword)
        {
            try
            {
                
                string search = "https://mittkok.expressen.se/sok/?q=" + searchword;

                httpclient = new HttpClient();
                var html = await httpclient.GetStringAsync(search);

                var htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(html);

                var receptLista = new List<HtmlNode>();
                receptLista = htmldoc.DocumentNode.Descendants("div")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("tile-item tile-item--recipe")).ToList();

                if (receptLista.Count > 10)
                {
                    int limit = receptLista.Count - 10;
                    receptLista.RemoveRange(9, limit);
                }
 
                foreach (var item in receptLista)
                {
                    string url = item.SelectSingleNode("a").GetAttributeValue("href", "");

                    using (HttpClient client = new HttpClient())
                    {

                        var html2 = await client.GetStringAsync(url);
                        HtmlDocument htmldoc2 = new HtmlDocument();
                        htmldoc2.LoadHtml(html2);

                        var root = new List<HtmlNode>();
                        root = htmldoc2.DocumentNode.Descendants("div")
                                        .Where(node => node.GetAttributeValue("class", "")
                                        .Equals("recipe")).ToList();

                        //getImage

                        string image = root[0].SelectSingleNode("div[2]/div[1]/div[2]/img").GetAttributeValue("src", "");

                        //getTitle

                        string title = root[0].SelectSingleNode("div[1]/div[1]/h1").InnerHtml;
                        title = convertUTF(title);

                        //getDescription

                        string description = root[0].SelectSingleNode("div[3]/div[1]/div[1]/p[1]").InnerHtml;
                        description = convertUTF(description);

                        description = limitDescription(title, description);

                        title = limitTitle(title);

                        //GetIngredienserToList

                        var ingredientRoot = root[0].Descendants("span")
                                                    .Where(node => node.GetAttributeValue("itemprop", "")
                                                    .Equals("recipeIngredient")).ToList();

                        var ingredientList = new List<Ingredient>();

                        foreach (HtmlNode nod in ingredientRoot)
                        {
                            string ingredientString = convertUTF(nod.InnerHtml);
                            ingredientList.Add(new Ingredient(ingredientString));
                        }

                        if (ingredientList.Count != 0)
                        {
                            var recipe = new Recipe(title, description, ingredientList, url, ingredientList.Count, image);

                            ListOfRecipes.Add(recipe);
                            SetRecipeMatches(recipe);
                        }
                    }
                }

                return ListOfRecipes;
            }
            catch (Exception)
            {
                return new List<Recipe>();
            }
            finally
            {
                
            }
        }

        public async Task<List<Recipe>> GetRecipesKoket(string searchword)
        {
            try
            {
                string search = "https://www.koket.se/search?searchtext=" + searchword;

                if (searchword.Contains("Vegetariskt "))
                {
                    searchword = searchword.Replace("Vegetariskt ", "");
                    search = "https://www.koket.se/search?searchtext=" + searchword + "&category_ids=3854";
                }
                else if (searchword.Contains("Veganskt ")) 
                {
                    searchword = searchword.Replace("Veganskt ", "");
                    search = "https://www.koket.se/search?searchtext=" + searchword + "&category_ids=234";
                }

                
                httpclient = new HttpClient();
                var html = await httpclient.GetStringAsync(search);

                var htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(html);

                var receptLista = new List<HtmlNode>();
                receptLista = htmldoc.DocumentNode.Descendants("article")
                                     .Where(node => node.GetAttributeValue("class", "")
                                     .Equals("list-item recipe ")).ToList();

                if (receptLista.Count > 11)
                {
                    int limit = receptLista.Count - 11;
                    receptLista.RemoveRange(10, limit);
                }

                foreach (var item in receptLista)
                {
                    string url = "https://www.koket.se" + item.SelectSingleNode("a[1]").GetAttributeValue("href", "");

                    using (HttpClient client = new HttpClient())
                    {

                        var html2 = await client.GetStringAsync(url);
                        HtmlDocument htmldoc2 = new HtmlDocument();
                        htmldoc2.LoadHtml(html2);

                        var root = new List<HtmlNode>();
                        root = htmldoc2.DocumentNode.Descendants("div")
                                       .Where(node => node.GetAttributeValue("id", "")
                                       .Equals("react-recipe-page-wrapper")).ToList();

                        //getImage

                        var imgRoot = new List<HtmlNode>();
                        imgRoot = root[0].Descendants("div")
                                         .Where(node => node.GetAttributeValue("class", "")
                                         .Equals("image-container ")).ToList();

                        string image = "";

                        if (imgRoot.Count != 0)
                        {
                            var imgRoot2 = new List<HtmlNode>();
                            imgRoot2 = imgRoot[0].Descendants("img").ToList();

                            if (imgRoot2.Count != 0)
                            {
                                image = imgRoot2[0].GetAttributeValue("src", "");
                            }
                        }
                        
                        //getTitle

                        var titleRoot = new List<HtmlNode>();
                        titleRoot = root[0].Descendants("h1")
                                         .Where(node => node.GetAttributeValue("itemprop", "")
                                         .Equals("name")).ToList();
                        string title = titleRoot[0].InnerHtml;
                        title = convertUTF(title);

                        //getDescription

                        var descRoot = new List<HtmlNode>();
                        descRoot = root[0].Descendants("div")
                                         .Where(node => node.GetAttributeValue("class", "")
                                         .Equals("description ")).ToList();

                        string description = descRoot[0].SelectSingleNode("div[1]/p").InnerHtml;
                        description = convertUTF(description);

                        description = limitDescription(title, description);

                        title = limitTitle(title);

                        //GetIngredienserToList

                        var ingredientRoot = root[0].Descendants("span")
                                                    .Where(node => node.GetAttributeValue("class", "")
                                                    .Equals("ingredient")).ToList();

                        var ingredientList = new List<Ingredient>();

                        foreach (HtmlNode nod in ingredientRoot)
                        {
                            string ingredientString = convertUTF(nod.InnerHtml);
                            ingredientList.Add(new Ingredient(ingredientString));
                        }

                        if (ingredientList.Count != 0)
                        {
                            var recipe = new Recipe(title, description, ingredientList, url, ingredientList.Count, image);

                            ListOfRecipes.Add(recipe);
                            SetRecipeMatches(recipe);
                        }

                    }
                }

                return ListOfRecipes;
            }
            catch (Exception)
            {
                return new List<Recipe>();
            }
        }

        private string convertUTF(string text)
        {

            if (text.Contains("&#246;"))
            {
                text = text.Replace("&#246;", "ö");
            }
            if (text.Contains("&#228;"))
            {
                text = text.Replace("&#228;", "ä");
            }
            if (text.Contains("&#229;"))
            {
                text = text.Replace("&#229;", "å");
            }
            if (text.Contains("&#214;"))
            {
                text = text.Replace("&#214;", "Ö");
            }
            if (text.Contains("&#196;"))
            {
                text = text.Replace("&#196;", "Ä");
            }
            if (text.Contains("&#197;"))
            {
                text = text.Replace("&#197;", "Å");
            }
            if (text.Contains("&#233;"))
            {
                text = text.Replace("&#233;", "é");
            }
            if (text.Contains("&#176;"))
            {
                text = text.Replace("&#176;", "°");
            }
            if (text.Contains("&#224;"))
            {
                text = text.Replace("&#224;", "à");
            }
            if (text.Contains("&#243;"))
            {
                text = text.Replace("&#243;", "ó");
            }
            if (text.Contains("&#232;"))
            {
                text = text.Replace("&#232;", "è");
            }
            if (text.Contains("&#244;"))
            {
                text = text.Replace("&#244;", "ô");
            }
            if (text.Contains("&amp;"))
            {
                text = text.Replace("&amp;", "&");
            }
            if (text.Contains("&#038;"))
            {
                text = text.Replace("&#038;", "&");
            }
            if (text.Contains("&#8211;"))
            {
                text = text.Replace("&#8211;", "&");
            }
            if (text.Contains("<strong>"))
            {
                text = text.Replace("<strong>", "");
            }
            if (text.Contains("</strong>"))
            {
                text = text.Replace("</strong>", "");
            }
            if (text.Contains("<span>"))
            {
                text = text.Replace("<span>", "");
            }
            if (text.Contains("</span>"))
            {
                text = text.Replace("</span>", "");
            }
            if (text.Contains("&#x27;"))
            {
                text = text.Replace("&#x27;", "'");
            }
            if (text.Contains("\n"))
            {
                text = text.Replace("\n", " ");
            }
            if (text.Contains("<br>"))
            {
                text = text.Replace("<br>", "");
            }
            if (text.Contains("&#8232;"))
            {
                text = text.Replace("&#8232;", "");
            }
            if (text.Contains("&#248;"))
            {
                text = text.Replace("&#248;", "ø");
            }
            if (text.Contains("&quot;"))
            {
                text = text.Replace("&quot;", "'");
            }


            return text;
        }
        
        public void SetRecipeMatches(Recipe recipe)
        {
            int numMatches = 0;
            var db = new DataBase();
            var list = new List<Ingredient>();
            list = db.GetIngredientsItems();

            foreach (var ingredient in recipe.Ingredients)
            {
                foreach (var item in list)
                {
                    string ownIngredient = item.Name.ToLower();
                    string recipeIngredient = ingredient.Name.ToLower();

                    if(recipeIngredient.Contains(ownIngredient))
                    {
                        numMatches++;
                        recipe.NumIngredients = numMatches;
             
                    }
                }
            }
        } 
           
        private string limitDescription(string title, string desc)
        {
            int characterLength = (title + desc).Length;
            int limit = 160;

            if (title.Length > 15)
            {
                limit = 140;
                if (title.Length > 22)
                {
                    limit = 127;

                    if (title.Length > 34)
                    {
                        limit = 115;

                        if (title.Length > 50)
                        {
                            limit = 100;

                            if (title.Length > 60)
                            {
                                limit = 87;

                                if (title.Length > 70)
                                {
                                    limit = 73;

                                    if (title.Length > 80)
                                    {
                                        return "";
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (characterLength > limit)
            {

                int lengthRemove = characterLength - limit;
                if (lengthRemove <= desc.Length)
                {
                    desc = desc.Substring(0, desc.Length - lengthRemove) + "...";
                }
                else
                {
                    desc = "...";
                }
            }

            return desc;
        }

        private string limitTitle(string title)
        {
            if (title.Length > 80)
            {
                title = title.Substring(0, 80) + "...";
            }
            return title;
        }

        public List<Recipe> ListSorter(List<Recipe> recipes)
        {
            List<Recipe> list = new List<Recipe>();
            foreach (var item in recipes)
            {
                item.Percentage = (decimal)item.NumIngredients / (decimal)item.NumInRecipe;
                list.Add(item);
                
            }

            return list.OrderByDescending(x => x.Percentage).ThenBy(i => i.NumInRecipe).ToList();

        }

        
    }
}
