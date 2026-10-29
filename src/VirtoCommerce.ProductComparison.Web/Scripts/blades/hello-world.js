angular.module('VirtoCommerce.ProductComparison')
    .controller('VirtoCommerce.ProductComparison.helloWorldController', ['$scope', 'VirtoCommerce.ProductComparison.webApi', function ($scope, api) {
        var blade = $scope.blade;
        blade.title = 'Product Comparison';

        blade.refresh = function () {
            api.get(function (data) {
                blade.title = 'product-comparison.blades.hello-world.title';
                blade.data = data.result;
                blade.isLoading = false;
            });
        };

        blade.refresh();
    }]);
