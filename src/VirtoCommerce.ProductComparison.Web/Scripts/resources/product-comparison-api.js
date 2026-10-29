angular.module('VirtoCommerce.ProductComparison')
    .factory('VirtoCommerce.ProductComparison.webApi', ['$resource', function ($resource) {
        return $resource('api/product-comparison');
    }]);
