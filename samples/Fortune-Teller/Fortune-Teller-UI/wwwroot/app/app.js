angular.module('fortunes', ['ngRoute']).config(function ($routeProvider) {

    $routeProvider.when('/', {
        templateUrl: 'fortune.html',
        controller: 'fortune'
    })

}).controller('fortune', function ($scope, $http) {

    $http.get('random').success(function (data) {
        $scope.fortune = data;
    });

});