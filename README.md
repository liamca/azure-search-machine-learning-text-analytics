---
services: search, machine-learning, text-analytics
platforms: dotnet
author: liamca
---

# Leverage Machine Learning, Text Analytics for Document Search Optimization in Azure Search

This sample demonstrates how to take a block of text, extract the key phrases and upload them to Azure Search.

<img src="https://raw.githubusercontent.com/liamca/azure-search-machine-learning-text-analytics/master/demo.png">

## Overview

Text Analytics allows for the analysis of unstructured text for tasks such as sentiment analysis, key phrase extraction, language detection and topic detection.   This sample shows how to use key phrase extraction with an Azure Search service which can be used to:

- Reduce the content size of the index to only key phrases
- Categorize key phrases to allow for faceting and filtering of these key terms

## Demo

In this demo, you will see how to:

- Upload a block of text to Azure Machine Learning Text Analytics API and return the set of key phrases
- Upload the resulting key phrases into an Azure Search index where each key phrase is stored within a Collection datatype (comma separated list of items)
- Search is performed against this populated search index and the individual phrases are returned





