// var HtmlWebpackPlugin = require("html-webpack-plugin");
var path = require("path");

module.exports = {
	devServer: {
		contentBase: "./public",
		historyApiFallback: true,
		port: 8080
	},
	entry: "./TryElmish.fsproj",
	// externals: {
	// 	react: "React"
	// },
	// mode: "development",
	module: {
		rules: [{ test: /\.fs(x|proj)?$/, use: "fable-loader" }]
	},
	output: {
		filename: "bundle.js",
		path: path.join(__dirname, "./public")
	}
	// plugins: [new HtmlWebpackPlugin()]
};
