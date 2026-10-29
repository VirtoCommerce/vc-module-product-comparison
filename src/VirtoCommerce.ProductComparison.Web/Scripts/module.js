// Call this to register your module to main application
var moduleName = 'VirtoCommerce.ProductComparison';

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .config(['$stateProvider',
        function ($stateProvider) {
            $stateProvider
                .state('workspace.ProductComparisonState', {
                    url: '/product-comparison',
                    templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
                    controller: [
                        'platformWebApp.bladeNavigationService',
                        function (bladeNavigationService) {
                            var newBlade = {
                                id: 'blade1',
                                controller: 'VirtoCommerce.ProductComparison.helloWorldController',
                                template: 'Modules/$(VirtoCommerce.ProductComparison)/Scripts/blades/hello-world.html',
                                isClosingDisabled: true,
                            };
                            bladeNavigationService.showBlade(newBlade);
                        }
                    ]
                });
        }
    ])
    .run(['platformWebApp.mainMenuService', '$state',
        function (mainMenuService, $state) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/product-comparison',
                icon: 'fa fa-cube',
                title: 'Product Comparison',
                priority: 100,
                action: function () { $state.go('workspace.ProductComparisonState'); },
                permission: 'product-comparison:access',
            };
            mainMenuService.addMenuItem(menuItem);
        }
    ]);
