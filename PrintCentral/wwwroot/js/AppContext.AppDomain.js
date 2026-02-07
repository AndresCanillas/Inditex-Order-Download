// =========================================================================
// Companies
// =========================================================================
// #region Companies

AppContext.Companies  = (function () {
	return {
		GetList: function (callback) {
			AppContext.HttpGet("/companies/getlist", callback);
        },

        FilterByName: function (data) {
            return AppContext.HttpPost("/companies/filterbyname", data);
        },
		GetAll: function (callback) {
			AppContext.HttpGet("/companies/getall", callback);
		}, 
		GetByID: function (id, callback) {
			AppContext.HttpGet(`/companies/getbyid/${id}`, callback);
		},
		Insert: function (data, callback) {
			AppContext.HttpPost("/companies/insert", data, callback);
		},
		Update: function (data, callback) {
			AppContext.HttpPost("/companies/update", data, callback);
		},
		UpdateOrderSorting: function (data, callback) {
			AppContext.HttpPost("/companies/updateordersorting", data, callback);
		},
		Delete: function (id, callback) {
			AppContext.HttpPost(`/companies/delete/${id}`, null, callback);
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/companies/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function () {
						reject();
					}
				);
			});
		},
		GetFtpAccount: function (id, callback) {
			AppContext.HttpGet(`/companies/GetFtpAccount/${id}`, callback);
		},
		SaveFtpAccount: function (accinfo, callback) {
			AppContext.HttpPost("/companies/SaveFtpAccount", accinfo, callback);
		},
		GetProvidersList: function (companyid, callback) {
			AppContext.HttpGet(`/companies/getproviderslist/${companyid}`, callback);
		},
		GetProjectCompany: function (projectid, callback) {
			AppContext.HttpGet(`/companies/getprojectcompany/${projectid}`, callback);
		},
		AssignRFIDConfig: function (companyid, configid, callback) {
			AppContext.HttpPost(`/companies/assignrfidconfig/${companyid}/${configid}`, null, callback, null, false, false);
		},
		GetERPConfigs: function (companyid) {
			return AppContext.HttpGet(`/companies/geterpconfig/${companyid}`);
		},
		SaveERPConfig: function (companyid, data) {
			return AppContext.HttpPost('companies/saveerpconfig', { CompanyID: companyid, Config: data}, null, null, true, true);
		},
		DelERPConfig: function (configid) {
			return AppContext.HttpPost(`companies/delerpconfig/${configid}`, null, null, true, true);
		}
	};
})();
// #endregion



// =========================================================================
// Locations
// =========================================================================
// #region Locations

AppContext.Locations = (function () {
	return {
		GetList: function (callback) {
			AppContext.HttpGet(`/locations/getlist`, callback);
		},
		GetByCompanyID: function (companyid, callback) {
			return AppContext.HttpGet(`/locations/getbycompanyid/${companyid}`, callback);
		},
		GetByID: function (id, callback) {
			AppContext.HttpGet(`/locations/getbyid/${id}`, callback);
		},
		Insert: function (data, callback) {
			AppContext.HttpPost(`/locations/insert`, data, callback);
		},
		Update: function (data, callback) {
			AppContext.HttpPost("/locations/update", data, callback);
		},
		Delete: function (id, callback) {
			AppContext.HttpPost(`/locations/delete/${id}`, null, callback);
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/locations/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function () {
						reject();
					}
				);
			});
		}
	};
})();
//#endregion



// =========================================================================
// Printers
// =========================================================================
// #region Printers

AppContext.Printers = (function () {
	return {
		GetByID: function (id, callback) {
			return AppContext.HttpGet(`/printers/getbyid/${id}`, callback);
		},
		GetList: function (callback) {
			return AppContext.HttpGet("/printers/getlist", callback);
		},
		GetByLocationID: function (locationid, callback) {
			return AppContext.HttpGet(`/printers/getbylocationid/${locationid}`, callback);
		},
		GetByCompanyID: function (companyid, callback) {
			return AppContext.HttpGet(`/printers/getbycompanyid/${companyid}`, callback);
		},
		GetStates: function (callback) {
			return AppContext.HttpGet("/printers/getstates", callback);
		},
		Insert: function (data, callback) {
			return AppContext.HttpPost(`/printers/insert`, data, callback);
		},
		Update: function (data, callback) {
			return AppContext.HttpPost("/printers/update", data, callback);
		},
		Delete: function (id, callback) {
			return AppContext.HttpPost(`/printers/delete/${id}`, null, callback);
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/printers/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function (result) {
						reject();
					}
				);
			});
		},
		GetSettings: function (printerid, articleid, callback) {
			return AppContext.HttpGet(`/printers/getsettings/${printerid}/${articleid}`, callback);
		},
		UpdateSettings: function (settings, callback) {
			return AppContext.HttpPost(`/printers/updatesettings`, settings, callback);
		},
		ChangeLocation: function (printerid, locationid, callback) {
			return AppContext.HttpPost(`/printers/changelocation/${printerid}/${locationid}`, null, callback);
		}
	};
})();
//#endregion



// =========================================================================
// Users
// =========================================================================
// #region Users
AppContext.Users = (function () {
	return {
		GetList: function (callback) {
			AppContext.HttpGet(`/users/getlist`, callback);
		},
		GetByCompanyID: function (companyid, callback) {
			AppContext.HttpGet(`/users/getbycompanyid/${companyid}`, callback);
		},
		GetByID: function (userid, callback) {
			AppContext.HttpGet(`/users/getbyid/${userid}`, callback);
		},
		Insert: function (data, callback) {
			AppContext.HttpPost("/users/insert", data, callback);
		},
		Update: function (data, callback) {
			AppContext.HttpPost("/users/update", data, callback);
		},
		Delete: function (id, callback) {
			AppContext.HttpPost(`/users/delete/${id}`, null, callback);
		},
		GetRoles: function (callback) {
			AppContext.HttpGet("/users/getroles", callback);
		},
		GetUserRoles: function (userid, callback) {
			AppContext.HttpGet(`/users/getuserroles/${userid}`, callback);
		},
		AddRole: function (userid, role, callback) {
			AppContext.HttpPost(`/users/addrole/${userid}/${role}`, null, callback);
		},
		RemoveRole: function (userid, role, callback) {
			AppContext.HttpPost(`/users/removerole/${userid}/${role}`, null, callback);
		},
		UpdateProfile: function (data, callback) {
			AppContext.HttpPost("/users/updateprofile", data, callback);
		},
		ResetPassword: function (id, callback) {
			AppContext.HttpPost(`/users/resetpwd/${id}`, null, callback);
        },
        GetCustomerList: function (companyid, isIDT, callback) {
            AppContext.HttpGet(`/users/getcustomerlist/${companyid}/${isIDT}`, callback);
		},
		GetResetPasswordURL: function (id, callback) {
			AppContext.HttpGet(`/users/getresetpwdurl/${id}`, callback);
		}
	};
})();
//#endregion



// =========================================================================
// RFIDConfig
// =========================================================================
// #region RFIDConfig
AppContext.RfidConfig = (function () {
	return {
		GetByCompanyID: function (companyid, callback) {
			AppContext.HttpGet(`/rfidconfig/getbycompanyid/${companyid}`, callback);
		},
		GetByBrandID: function (brandid, callback) {
			AppContext.HttpGet(`/rfidconfig/getbybrandid/${brandid}`, callback);
		},
		GetByProjectID: function (projectid, callback) {
			AppContext.HttpGet(`/rfidconfig/getbyprojectid/${projectid}`, callback);
		},
		Update: function (data, callback) {
			AppContext.HttpPost("/rfidconfig/update", data, callback);
		},
		UpdateSequence: function (id, serial, callback) {
			AppContext.HttpPost(`/rfidconfig/updatesequence/${id}/${serial}`, null, callback);
		}
	};
})();
//#endregion


// =========================================================================
// OrderWorkflowConfig
// =========================================================================
// #region OrderWorkflowConfig
AppContext.OrderWorkflowConfig = (function () {
	return {
		GetByProjectID: function (projectid, callback) {
			AppContext.HttpGet(`/orderworkflowconfig/getbyprojectid/${projectid}`, callback);
		},
		Update: function (data, callback) {
			AppContext.HttpPost("/orderworkflowconfig/update", data, callback);
		}
	};
})();
//#endregion


// =========================================================================
// Packs
// =========================================================================
// #region Packs
AppContext.Packs = (function () {
	return {
		GetList: function (callback) {
			AppContext.HttpGet("/packs/getlist", callback);
		},
		GetByCompanyID: function (companyid, callback) {
			AppContext.HttpGet(`/packs/getbycompanyid/${companyid}`, callback);
		},
		GetByProjectID: function (projectid, callback) {
			return AppContext.HttpGet(`/packs/getbyprojectid/${projectid}`, callback);
		},
		GetByID: function (id, callback) {
			AppContext.HttpGet(`/packs/getbyid/${id}`, callback);
		},
		Insert: function (data, callback) {
			AppContext.HttpPost("/packs/insert", data, callback);
		},
		Update: function (data, callback) {
			AppContext.HttpPost("/packs/update", data, callback);
		},
		Delete: function (id, callback) {
			AppContext.HttpPost(`/packs/delete/${id}`, null, callback);
		},

		ExpandPackByOrderId: function (orderid, projectid, packcode) {
			return AppContext.HttpPost(`packs/expandpackbyorderid/${orderid}/${projectid}/${packcode}`, null, null, null, true )
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/packs/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function () {
						reject();
					}
				);
			});
		},
		AddArticleToPack: function (packid, articleid, callback) {
			AppContext.HttpPost(`/packs/addarticletopack/${packid}/${articleid}`, null, callback);
        },
        AddArticleByData: function (data, callback) {
            AppContext.HttpPost(`/packs/addarticlebydata`, data, callback);
        },
        AddArticleByPlugin: function (data, callback) {
            AppContext.HttpPost(`/packs/addarticlebyplugin`, data, callback);
        },
		RemoveArticleFromPack: function (packid, articleid, id, callback) {
			AppContext.HttpPost(`/packs/removearticlefrompack/${packid}/${articleid}/${id}`, null, callback);
        },
        UpdatePackArticle: function (packId, articleId, quantity, id, callback) {
            AppContext.HttpPost(`/packs/updatepackarticle/${packId}/${articleId}/${quantity}/${id}`, null, callback);
        },
        GetPackArticleById: function (id, callback) {
            AppContext.HttpGet(`/packs/getpackarticlebyid/${id}`, callback);
        },
	};
})();
//#endregion



// =========================================================================
// Articles
// =========================================================================
// #region Articles

AppContext.Articles = (function () {
	return {
		GetList: function (callback) {
			AppContext.HttpGet("/articles/getlist", callback);
		},
		GetFullByProjectID: function (projectid, callback) {
			return AppContext.HttpGet(`/articles/getfullbyprojectid/${projectid}`, callback);
		},
		GetFullByProjectIDFiltered: function (data, callback) {
			return AppContext.HttpPost(`/articles/getfullbyprojectidfiltered/`, data, callback);
		},
		GetByPackID: function (packid, callback) {
			AppContext.HttpGet(`/articles/getbypackid/${packid}`, callback);
		},
		GetByProjectID: function (projectid, callback) {
			AppContext.HttpGet(`/articles/getbyprojectid/${projectid}`, callback);
		},
		GetByID: function (id, callback) {
			AppContext.HttpGet(`/articles/getbyid/${id}`, callback);
		},
		GetByArticleCode: function (articlecode, callback) {
			AppContext.HttpGet(`/articles/getbycode/${articlecode}`, callback);
		},
		GetFullArticle: function (id, callback) {
			AppContext.HttpGet(`/articles/getfull/${id}`, callback);
		},
		Insert: function (data, callback) {
			AppContext.HttpPost("/articles/insert", data, callback);
		},
		Update: function (data, callback) {
			AppContext.HttpPost("/articles/update", data, callback);
		},
		Delete: function (id, callback) {
			AppContext.HttpPost(`/articles/delete/${id}`, null, callback);
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/articles/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function () {
						reject();
					}
				);
			});
		},

		AddArtifact: function (artifactData) {
			return AppContext.HttpPost("/articles/addartifact/", artifactData);
		},

		UpdateArtifact: function (artifactData) {
            return AppContext.HttpPost("/article/updateartifact/", artifactData, null, null, true);
		},

		RemoveArtifact: function (artifactData) {
			return AppContext.HttpPost("/articles/removeartifact/", artifactData);
		},

		GetArtifacts: function (articleId) {
			return AppContext.HttpGet(`/articles/getartifacts/${articleId}`);
        },

        GetArtifactsByArticles: function (data, callback) {
            return AppContext.HttpPost(`/articles/getartifactsbyarticles/`, data, callback);
        },

        GetArticlesWithLabels: function (postdata,projectid) {
            return AppContext.HttpPost(`/articles/getarticleswithlabels/${projectid}`, postdata, null, null, true, true);
		}, 

        GetArticleCompositionConfig: function (projectid, articleid, callback) {
			return AppContext.HttpGet(`/articles/compostionConfig/${projectid}/${articleid}`, callback);
		}, 

		SaveArticleCompositionConfig: function (postData, callback) {
			return AppContext.HttpPost(`/articles/savecompositionconfig/`, postData, callback);
		},

		SaveArticleAccessBlockConfig: function (postData, callback) {
			return AppContext.HttpPost(`/articles/saveaccessblockconfig/`, postData, callback);
		}
        
	};
})();

// #endregion

// =========================================================================
// Addresses
// =========================================================================
// #region Addresses

AppContext.Addresses = (function () {
    return {
        GetList: function (callback) {
            AppContext.HttpGet("/addresses/getlist", callback);
        },
        GetByCompany: function (id, callback) {
            return AppContext.HttpGet(`/addresses/getbycompany/${id}`, callback);
        },
        GetByID: function (id, callback) {
            return AppContext.HttpGet(`/addresses/getbyid/${id}`, callback);
        },
        Insert: function (data, companyId, callback) {
            AppContext.HttpPost(`/addresses/insert/${companyId}`, data, callback);
        },
        Update: function (data, callback) {
            return AppContext.HttpPost("/addresses/update", data, callback);
        },
        Delete: function (id, callback) {
            AppContext.HttpPost(`/addresses/delete/${id}`, null, callback);
        },
        GetDefaultByCompany: function (id, callback) {
            return AppContext.HttpGet(`/addresses/getdefaultbycompany/${id}`, callback);
        },
        SetDefaultAddress: function (companyId, id, isDefault, callback) {
            AppContext.HttpPost(`/addresses/setdefaultaddress/${companyId}/${id}/${isDefault}`, null, callback);
        }
    };
})();

// #endregion



// =========================================================================
// Materials
// =========================================================================
// #region Materials
AppContext.Materials = (function () {
	return {
		GetList: function (callback) {
			AppContext.HttpGet("/materials/getlist", callback);
		},
		GetByID: function (id, callback) {
			AppContext.HttpGet(`/materials/getbyid/${id}`, callback);
		},
		Insert: function (data, callback) {
			AppContext.HttpPost("/materials/insert", data, callback);
		},
		Update: function (data, callback) {
			AppContext.HttpPost("/materials/update", data, callback);
		},
		Delete: function (id, callback) {
			AppContext.HttpPost(`/materials/delete/${id}`, null, callback);
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/materials/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function () {
						reject();
					}
				);
			});
		}
	};
})();

// #endregion



// =========================================================================
// Labels
// =========================================================================
// #region Labels
AppContext.Labels = (function () {
	return {
		GetList: function (callback) {
			AppContext.HttpGet("/labels/getlist", callback);
		},
		GetByProjectID: function (projectid, callback) {
			AppContext.HttpGet(`/labels/getbyprojectid/${projectid}`, callback);
		},
		GetByID: function (id, callback) {
			AppContext.HttpGet(`/labels/getbyid/${id}`, callback);
		},
		Insert: function (data, callback) {
			AppContext.HttpPost("/labels/insert", data, callback);
		},
		Update: function (data, callback) {
			AppContext.HttpPost("/labels/update", data, callback);
		},
		Delete: function (id, callback) {
			AppContext.HttpPost(`/labels/delete/${id}`, null, callback);
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/labels/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function () {
						reject();
					}
				);
			});
		},
		SetLabelPreviewWithVariables: function (id, data, callback) {
			AppContext.HttpPost(`/labels/setpreviewwithvariables/${id}`, data, callback);
		},
		GetLabelInfo: function (id, callback) {
			AppContext.HttpPost(`/labels/info/${id}`, null, callback);
		},
		CatalogFromLabelVariables: function (variables) {
			var idx = 0;
			var result = [];
			for (var v of variables) {
				result.push({
					FieldID: idx++,
					Name: v.Name,
					Description: v.Description,
					Type: GetColumnType(v.VariableType),
					Length: v.Length,
					CanBeEmpty: !v.IsValueRequired
				});
			}
			return result;

			function GetColumnType(vt) {
				switch (vt) {
					case 0: return 7;
					case 1: return 7;
					case 2: return 6;
					case 3: return 7;
					case 4: return 4;
					case 5: return 4;
					case 6: return 2;
				}
			}
		},
		CreateDefaultPreviewModel: function (variables) {
			var result = {};
			for (var v of variables) {
				result[v.Name] = v.DefaultValue;
			}
			return result;
        },
        UpdateGroupingFields: function (id, data, callback) {
            return AppContext.HttpPost(`/labels/updategroupingfields/${id}/${data}`, null, callback);
        },
        UpdateComparerField: function (id, data, callback) {
            return AppContext.HttpPost(`/labels/updatecomparerfield/${id}/${data}`, null, callback);
        }
	};
})();
//#endregion



// =========================================================================
// VariableData
// =========================================================================
// #region VariableData
AppContext.VariableData = (function () {
	return {
		GetByID: function (projectid, id, callback) {
			return AppContext.HttpGet(`/variabledata/getbyid/${projectid}/${id}`, callback);
		},
		GetByCode: function (projectid, productCode, callback) {
			AppContext.HttpGet(`/variabledata/getbycode/${projectid}/${productCode}`, callback, function () { callback(null); });
        },
        GetByDetail: function (projectid, id) {
            return AppContext.HttpGet(`/variabledata/getbydetail/${projectid}/${id}`);
        },
	};
})();
//#endregion



// =========================================================================
// Providers
// =========================================================================
// #region Providers

AppContext.Providers = (function () {
	return {
		GetByCompanyID: function (companyid, callback) {
			return AppContext.HttpGet(`/providers/getbycompanyid/${companyid}`, callback);
		},
		UpdateProvider: function (data, callback) {
			AppContext.HttpPost(`/providers/update`, data, callback);
		},
		AddProviderToCompany: function (companyid, data, callback) {
			AppContext.HttpPost(`/providers/addprovider/${companyid}`, data, callback);
		},
		RemoveProviderFromCompany: function (providerid, callback) {
			AppContext.HttpPost(`/providers/removeprovider/${providerid}`, null, callback);
		},
		CheckIsBroker: function (companyid) {
			return AppContext.HttpGet(`/companies/checkisbroker/${companyid}`);
		},
		GetArticlesByCompanyId: function (companyid, callback) {
			return AppContext.HttpGet(`/providers/getarticlesbycompanyid/${companyid}`, callback);
		},
		RemoveArticleFromProvider: function (articledetailid, callback) {
			AppContext.HttpPost(`/providers/removearticlefromprovider/${articledetailid}`, null, callback);
		},
		AddArticleToProvider: function (articleid, providerid, callback) {
			AppContext.HttpPost(`/providers/addarticlefromprovider/${articleid}/${providerid}`, null, callback);
		},
		AddArticlesToProvider: function (articleid, providerid, callback) {
			AppContext.HttpPost(`/providers/addarticlesfromprovider/${articleid}/${providerid}`, null, callback);
		},
		GetArticlesByProviderId: function (providerid,companyid ,callback) {
			return AppContext.HttpGet(`/providers/getarticlesdetailbyproviderid/${providerid}/${companyid}`, callback);
		},
		RemoveAllArticlesFromProvider: function (companyid, providerid, callback) {
			return AppContext.HttpPost(`/providers/removeallarticlefromprovider/${companyid}/${providerid}`, callback);
		},

	};
})();

// #endregion



// =========================================================================
// PrinterJobs
// =========================================================================
// #region PrinterJobs
AppContext.PrinterJobs = (function () {
	return {
		GetByID: function (jobid, callback) {
			AppContext.HttpGet(`/printerjobs/getbyid/${jobid}`, callback);
		},
		StartJob: function (jobid, callback) {
			AppContext.HttpPost(`/printerjobs/startjob/${jobid}`, null, callback);
		},

		PauseJob: function (jobid, callback) {
			AppContext.HttpPost(`/printerjobs/pausejob/${jobid}`, null, callback, null, true);
		},

		CancelJob: function (jobid, callback) {
			AppContext.HttpPost(`/printerjobs/canceljob/${jobid}`, null, callback, null, true);
		},

		AddExtras: function (jobid, detailid, quantity, callback) {
			AppContext.HttpPost(`/printerjobs/addextras`, { JobID: jobid, DetailID: detailid, Quantity: quantity }, callback);
		},

		PrintSample: function (projectid, printerid, articleid, orderid, productid) {
			AppContext.HttpPost(`PrinterJobs/PrintSample/${projectid}/${printerid}/${articleid}/${orderid}/${productid}`, null, function () { });
		},

		AssignPrinter: function (jobid, printerid, callback) {
			AppContext.HttpPost(`PrinterJobs/ChangePrinter/${jobid}/${printerid}`, null, callback, null, true);
		},

		ActivateJob: function (jobid, callback) {
			AppContext.HttpPost(`/printerjobs/Activatejob/${jobid}`, null, callback);
		},

		ResetJob: function (jobid, callback) {
			AppContext.HttpPost(`/printerjobs/resetjob/${jobid}`, null, callback);
		},

		SetDetailProgress: function (detailid, progress, callback) {
			AppContext.HttpPost(`/printerjobs/SetDetailProgress/${detailid}/${progress}`, null, callback);
		},

		FilterPrinterJobs: function (filter, callback) {
			AppContext.HttpPost('/printerjobs/FilterPrinterJobs', filter, callback);
		},

		GetJobDetails: function (id, applySort, callback) {
			AppContext.HttpGet(`/PrinterJobs/GetJobDetails/${id}/${applySort}`, callback);
		}
	};
})();
//#endregion



// =========================================================================
// Mappings
// =========================================================================
// #region Mappings
AppContext.Mappings = (function () {
	return {
		GetList: function (callback) {
			return AppContext.HttpGet(`/mappings/getlist`, callback);
		},
		GetByCompanyID: function (companyid, callback) {
			return AppContext.HttpGet(`/mappings/getbycompanyid/${companyid}`, callback);
		},
		GetByProjectID: function (projectid, callback) {
			AppContext.HttpGet(`/mappings/getbyprojectid/${projectid}`, callback);
		},
		GetByID: function (id, callback) {
			return AppContext.HttpGet(`/mappings/getbyid/${id}`, callback);
		},
		Insert: function (data, callback) {
			return AppContext.HttpPost(`/mappings/insert`, data, callback);
		},
		Update: function (data, callback) {
			return AppContext.HttpPost("/mappings/update", data, callback);
		},
		Delete: function (id, callback) {
			return AppContext.HttpPost(`/mappings/delete/${id}`, null, callback);
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/mappings/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function () {
						reject();
					}
				);
			});
		},
		GetColumnMappings: function (id, callback) {
			return AppContext.HttpGet(`/mappings/getcolumns/${id}`, callback);
		},
		AddColumnMapping: function (id, callback) {
			return AppContext.HttpPost(`/mappings/addcolumn/${id}`, null, callback);
		},
		InsertColumnMapping: function (id, position) {
			return AppContext.HttpPost(`/mappings/insertcolumn/${id}/${position}`);
		},
		MoveColumnDown: function (id) {
			return AppContext.HttpPost(`/mappings/movecolumndown/${id}`, null, null, null, false, false);
		},
		MoveColumnUp: function (id) {
			return AppContext.HttpPost(`/mappings/movecolumnup/${id}`, null, null, null, false, false);
		},
		DeleteColumnMapping: function (colid, callback) {
			return AppContext.HttpPost(`/mappings/deletecolumn/${colid}`, null, callback);
		},
		SaveColumnMappings: function (data, callback) {
			return AppContext.HttpPost("/mappings/save", data, callback, null, false, false);
		},
		InitMappings: function (mappingid, catalogid, callback) {
			return AppContext.HttpPost(`/mappings/init/${mappingid}/${catalogid}`, null, callback);
		},
		Duplicate: function (mappingid, duplicateName) {
			return AppContext.HttpPost(`/mappings/duplicate/${mappingid}/${duplicateName}`);
		}
	};
})();
//#endregion


// Brands
// =========================================================================
// #region Brands
AppContext.Brands = (function () {
    return {
        GetByCompanyID: function (companyid, callback) {
            AppContext.HttpGet(`/brands/getbycompanyid/${companyid}`, callback);
        },
        GetByID: function (id, callback) {
            AppContext.HttpGet(`/brands/getbyid/${id}`, callback);
        },
        Insert: function (data, callback) {
            AppContext.HttpPost("/brands/insert", data, callback);
        },
        Update: function (data, callback) {
            AppContext.HttpPost("/brands/update", data, callback);
        },
        Delete: function (id, callback) {
            AppContext.HttpPost(`/brands/delete/${id}`, null, callback);
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/brands/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function (result) {
						reject();
					}
				);
			});
		},
		AssignRFIDConfig: function (brandid, configid, callback) {
			AppContext.HttpPost(`/brands/assignrfidconfig/${brandid}/${configid}`, null, callback, null, false, false);
        }
    };

})();
//#endregion


// =========================================================================
// Projects
// =========================================================================
// #region Projects

AppContext.Projects = (function () {
	return {
		GetByID: function (id, callback) {
			return AppContext.HttpGet(`/projects/getbyid/${id}`, callback);
		},
		GetList: function (callback) {
			return AppContext.HttpGet("/projects/getlist", callback);
		},
        GetByBrandID: function (brandid, showAll, callback) {
            if (typeof showAll == "function") {
                callback = showAll;
                showAll = false;
            }
            return AppContext.HttpGet(`/projects/getbybrand/${brandid}/${showAll}`, callback);
		},
		Insert: function (data, callback) {
			return AppContext.HttpPost(`/projects/insert`, data, callback);
		},
		Update: function (data, callback) {
			return AppContext.HttpPost("/projects/update", data, callback);
		},
		Delete: function (id, callback) {
			return AppContext.HttpPost(`/projects/delete/${id}`, null, callback);
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/projects/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function (result) {
						reject();
					}
				);
			});
        },
        Hide: function (id, callback) {
            return AppContext.HttpPost(`/projects/hide/${id}`, null, callback);
        },
		GetFields: function (projectid, callback) {
			return AppContext.HttpPost(`/projects/fields/${projectid}`, null, callback);
		},
		AssignRFIDConfig: function (projectid, configid, callback) {
			return AppContext.HttpPost(`/projects/assignrfidconfig/${projectid}/${configid}`, null, callback, null, false, false);
        },
		AssignOrderWorkflowConfig: function (projectid, configid, callback) {
			return AppContext.HttpPost(`/projects/AssignOrderWorkflowConfig/${projectid}/${configid}`, null, callback, null, false, false);
		}
	};
})();
//#endregion



// =========================================================================
// Catalogs
// =========================================================================
// #region Catalogs

AppContext.Catalogs = (function () {
	return {
        GetByID: function (id, callback) {
			AppContext.HttpGet(`/catalogs/getbyid/${id}`, (data) => {
				callback(data);
			});
		},
		GetList: function (callback) {
			AppContext.HttpGet("/catalogs/getlist", callback);
		},
        GetByCatalogID: function (catalogid, callback) {
			AppContext.HttpGet(`/catalogs/getbycatalogid/${catalogid}`, (data) => {
				callback(data);
			});			
		},
		GetByProjectID: function (projectid, callback) {
			AppContext.HttpGet(`/catalogs/getbyproject/${projectid}`, callback);
		},
		GetByProjectIDWithRoles: function (projectid, callback) {
			AppContext.HttpGet(`/catalogs/getbyprojectwithroles/${projectid}`, callback);
		},
		GetByName: function (projectid, name, callback, errorHandler, showOverLay, successMessage) {
			return AppContext.HttpPost(`/catalogs/getbyname/${projectid}`, name, callback, errorHandler, showOverLay, successMessage);
		},
		Insert: function (data, callback) {
			AppContext.HttpPost(`/catalogs/insert`, data, callback);
		},
        Update: function (data, callback) {
			AppContext.HttpPost("/catalogs/update", data, callback);
		},
		Delete: function (id, callback) {
			AppContext.HttpPost(`/catalogs/delete/${id}`, null, callback);
		},
		Rename: function (id, value) {
			return new Promise(function (resolve, reject) {
				AppContext.HttpPost(`/catalogs/rename/${id}/${value}`, null,
					function (result) {
						resolve(result.Success);
					},
					function (result) {
						reject();
					}
				);
			});
		},
		AssignRoles: function (catalogid, roles, callback) {
			AppContext.HttpPost("catalogs/assignroles", roles, callback);
		}
	};
})();
//#endregion



// =========================================================================
// CatalogData
// =========================================================================
// #region CatalogData

AppContext.CatalogData = (function () {
	return {
		GetByID: function (catalogid, id, callback) {
			AppContext.HttpGet(`/catalogdata/getbyid/${catalogid}/${id}`, callback);
		},
		GetByCatalogID: function (catalogid, callback, errorHandler, showOverlay) {

			if (showOverlay == undefined) showOverlay = true;

			return AppContext.HttpGet(`/catalogdata/getbycatalog/${catalogid}`, callback, errorHandler, showOverlay);
		},

		GetCountByCatalogID: function (catalogid, callback, errorHandler, showOverlay) {

			if (showOverlay == undefined) showOverlay = true;

			return AppContext.HttpGet(`/catalogdata/getcountbycatalog/${catalogid}`, callback, errorHandler, showOverlay);
		},


		GetPageByCatalogID: function (catalogid, pageNumber, pageSize,callback, errorHandler, showOverlay) {

			if (showOverlay == undefined) showOverlay = true;

			return AppContext.HttpGet(`/catalogdata/getpagebycatalog/${catalogid}/${pageNumber}/${pageSize}`, callback, errorHandler, showOverlay);
		},
		GetSubset: function (catalogid, id, fieldName, callback) {
			AppContext.HttpGet(`/catalogdata/getsubset/${catalogid}/${id}/${fieldName}`, callback, null, true);
        },
        GetFullSubset: function (catalogid, fieldName, callback) {
            return AppContext.HttpGet(`/catalogdata/getfullsubset/${catalogid}/${fieldName}`, callback, null, true);
        },
		Insert: function (data, callback) {
			AppContext.HttpPost(`/catalogdata/insert`, data, callback, null, false, false);
        },
        AddSet: function (leftCatalogId, catalogId, parentId, selectedId, callback) {
            AppContext.HttpPost(`/catalogdata/addset`, { CatalogID: catalogId, RowID: selectedId, ParentRowID: parentId, LeftCatalogID: leftCatalogId }, callback, null, false, false);
        },
		Update: function (data, callback) {
			if(data.ID == 0)
				AppContext.HttpPost("/catalogdata/insert", data, callback, null, false, false);
			else
				AppContext.HttpPost("/catalogdata/update", data, callback, null, false, false);
		},
		Delete: function (catalogid, rowid, leftCatalogId,parentid, callback) {
            AppContext.HttpPost(`/catalogdata/delete`, { CatalogID: catalogid, RowID: rowid, LeftCatalogID: leftCatalogId, ParentRowID: parentid }, callback);
		},
		NewRecord: function (def) {
			var rec = {};
			for (var i = 0; i < def.length; i++) {
				rec[def[i].Name] = getDefaultValue(def[i].Type);
				if (def[i].Type == 1)
					rec[`_${def[i].Name}_DISP`] = "";
			}
			return rec;
		},
		SearchFirst: function (catalogid, fieldName, search, callback) {
			AppContext.HttpGet(`/catalogdata/searchfirst/${catalogid}/${fieldName}/${search}`, callback);
		},
		FreeTextSearch: function (catalogid, filter, callback) {
			AppContext.HttpPost(`/catalogdata/searchcatalog/${catalogid}`, filter, callback, null, true);
		},
		FreeTextSearchCount: function (catalogid, filter, callback) {
			AppContext.HttpPost(`/catalogdata/searchcatalogcount/${catalogid}`, filter, callback, null, true);
		},
		FreeTextSearchPaging: function (catalogid, pagenumber, pagesize, filter, callback) {
			AppContext.HttpPost(`/catalogdata/searchcatalogpaging/${catalogid}/${pagenumber}/${pagesize}`, filter, callback, null, true);
		},
		SubsetFreeTextSearch: function (catalogid, id, fieldName, filter, callback) {
			AppContext.HttpPost(`/catalogdata/searchsubset/${catalogid}/${id}/${fieldName}`, filter, callback, null, true);
		},
        DeleteAll: function (catalogid, callback) {
            AppContext.HttpPost(`/catalogdata/deleteall/${catalogid}`, { catalogid: catalogid }, callback);
		},
		SearchMultiple: function (catalogid, barcodes, callback) {
			AppContext.HttpPost(`/catalogdata/searchmultiple/${catalogid}`, barcodes, callback, null, true);
		},
		GetBaseDataByOrderID: function (projectid, orderid, callback) {
			AppContext.HttpGet(`/catalogdata/getbasedatabyorderid/${projectid}/${orderid}`, callback, null, true);
		}
	};

	function getDefaultValue(type) {
		switch (parseInt(type, 10)) {
			case 1: return null;
			case 2: return 0;
			case 3: return 0;
			case 4: return 0;
			case 5: return false;
			case 6: return new Date();
			case 7: return "";
			case 8: return 0;
			case 9: return null;
			case 10: return 0;
			default: return null;
		}
	}
})();
//#endregion

// =========================================================================
// Billing Info
// =========================================================================
// #region Billings

AppContext.Billings = (function () {
    return {
        GetByProviderID: function (id, callback) {
            AppContext.HttpGet(`/billings/getbyprovider/${id}`, callback);
        }
    };
})();

// #endregion


// =========================================================================
// Images Configuration
// =========================================================================
// #region Images

AppContext.ImageData = (function () {

	return {
		Insert: function (data) {
			return AppContext.HttpPost(`/images/insert`, data, null, null, true, true);
		},

		Update: function (data) {
			return AppContext.HttpPost(`/images/update`, data, null, null, true, true);
		},

		Delete: function (data) {
			return AppContext.HttpPost(`/images/delete`, data, null, null, true, true)
		},

		GetByProjectID: function (projectid) {
			return AppContext.HttpGet(`/images/getbyprojectid/${projectid}`);
		},

		GetByID: function (id) {
			return AppContext.HttpGet(`/images/getbyid/${id}`);
		}
	}

})();

// #endregion Images


// =========================================================================
// Admin Orders
// =========================================================================
// #region Admin Orders

AppContext.Orders = function () {

	// order get params
	// { filter: [ { type: 'OR|AND', field: 'FieldName', value: 'value',   }  ] }

	return {

		Get: function (data) {
			return AppContext.HttpPost('/orders/getreport', data)
		},

		GetOrderGroups: function (filter) {
            return AppContext.HttpPost('/groups/orders/getreport', filter, null, () => { });
        },

        GetFileCSVReport: function (filter) {
            return AppContext.HttpPost('/orders/FileCsvReport', filter, null, null, false, true);
		},

		GetDeliveryReport: function (filter) {
			return AppContext.HttpPost('/orders/DeliveryReport', filter, null, null, false, true);
		},

		// Este metodo creo que se esta utilizando una vez [OrderDetailView], para mostrar el detalle de la orden
		// no muestra los articulos extras, voy a agregar uno nuevo [GetAllArticles] para eviar errores
		GetArticles: function (orderID) {
			return AppContext.HttpGet(`/orders/getarticledetails/${orderID}`);
		},


		GetExtras: function (orderID) {
			// OrderArticlesFilter -> ArticleType : 0 -> All, 1 -> labels, 2 -> Items
			var filter = { OrderID: orderID, ArticleType: 2}

			return AppContext.HttpPost(`/orders/getarticledetails/`,filter);
		},

		GetAllArticles: function (orderGroupID) {
			var filter = { OrderGroupID: orderGroupID, ArticleType: 0 };
			return AppContext.HttpPost(`/orders/getarticledetails/`, filter);
		},

		GetLabels: function (orderID) {
			// OrderArticlesFilter -> ArticleType : 0 -> All, 1 -> labels, 2 -> Items
			var filter = { OrderID: orderID, ArticleType: 1 }

			return AppContext.HttpPost(`/orders/getarticledetails/`, filter);
		},

		GetInConflict: function (filter) {

			filter.IsInConflict = 1;

			return this.Get(filter);
		},

		GetBilled: function () {
			filter.IsBilled = 1;

			return this.Get(filter);
		},

		GetNoBilled: function () {
			filter.IsBilled = 0;

			return this.Get(filter);
		},

		GetStopped: function () {
			filter.IsStopped = 1;

			return this.Get(filter);
		},

		ChangeStatus: (data) => {
			return AppContext.HttpPost('/orders/actions/changestatus', data, null, null, false, true);
        },

        ChangeStatusByBroup: (data) => {
            return AppContext.HttpPost('/orders/actions/changestatusbygroup', data, null, null, false, false);
        },

		StopOrder: function (data) {
			// remove this option, replaced by StopByGroup
			return AppContext.HttpPost('/orders/actions/stop', data, null, null, false, true);
		},

		StopByGroup: function (data) {
			return AppContext.HttpPost('/orders/actions/stopbygroup', data, null, null, false, true);
		},

		ResumeOrder: function (data) {
			// remove this option, replaced by ContinueByGroup
			return AppContext.HttpPost('/orders/actions/resume', data, null, null, false, true);
        },

		ContinueByGroup: function (data) {
			return AppContext.HttpPost('/orders/actions/continuebygroup', data, null, null, false, true);
        },

        MoveByGroup: function (data) {
            return AppContext.HttpPost('/orders/actions/movebygroup', data, null, null, false, true);
        },

        CloneByGroup: function (data) {
            return AppContext.HttpPost('/orders/actions/clonebygroup', data, null, null, false, true);
        },

		GetLog: function (orderID) {
			return AppContext.HttpGet(`/orders/actions/getlog/${orderID}`);
		},

		GetOrderGroupLog: function (orderGroupID) {
			return AppContext.HttpGet(`/orders/actions/getgrouplog/${orderGroupID}`);
		},

		ResetValidation: function (postdata) {
			return AppContext.HttpPost('/orders/actions/resetvalidationgroup', postdata, null, null, true, true);
		},

        OpenDocumentPreview: function (orderID, articleCode) {
            window.open('/order/validate/getpreview/' + orderID + "-" + articleCode);
        },

        OpenProdSheet: function (orderID, articleCode) {
            var url = '/order/validate/getprodsheet/';
            window.open(url + orderID + "-" + articleCode);
        },

        CreateOrderDetail: function (orderID, articleCode) {
            var url = `/order/validate/createorderdetail/${orderID}-${articleCode}`;
            AppContext.HttpPost(url, null, null, null, true, true);
            //window.open(url + orderID + "-" + articleCode);
        },

        GetOrderDetail: function (orderID, articleCode) {
            var url = '/order/validate/getorderdetail/';
            window.open(url + orderID + "-" + articleCode);
        },

		ChangeProvider: function (postdata) {
			return AppContext.HttpPost('/orders/actions/changeprovider', postdata, null, null, true, true);
		},

		ChangeProductionType: function (postdata) {
			return AppContext.HttpPost('/orders/actions/changeproductiontype', postdata, null, null, true, true);
        },

        Derive: function (orderID, provideID, articleCode) {
            return AppContext.HttpPost(`/orders/actions/derive/${orderID}/${provideID}/${articleCode}`);
        },

        GetByID: function (id) {
            return AppContext.HttpGet(`/orders/getbyid/${id}`,null, null, false)
        },

        GetSimplePrintDetailsData: function (orderid, labelid) {
            return AppContext.HttpGet(`/orders/simpleprintdetailsdata/${orderid}/${labelid}`, null, null, true);
        },
        ChangeDueDate: function (data) {
            return AppContext.HttpPost("/orders/updateduedate", data, null, null, true, true);
        },

        GetCustomReport: function (filter) {
			return AppContext.HttpPost('/orders/customreport', filter, null, null,null, true);
        },

        GetFileCSVCustomReport: function (filter) {
            return AppContext.HttpPost('/orders/FileCsvCustomReport', filter);
		},
		GetOrderPdf: function (orderGroupID,orderNumber) {
			return AppContext.HttpGet(`/orders/getorderpdf/${orderGroupID}/${orderNumber}`, null,null, true);
		},
		GetCountryByOrderLocation: function (orderGroupID) {
			return AppContext.HttpGet(`/orders/getcountrybyorderlocation/${orderGroupID}`, null, null, true);
		}

	};

}();

// #endregion Admin Orders


// =========================================================================
// Wizards
// =========================================================================
// #region Wizards

AppContext.Wizard = function () {

	// #region customer callbacks and ajax request 
	function Success(overlay, callback, showSuccessMessage, data, status, rq) {
		if (overlay != null)
			overlay.remove();
		if (data) {
			var msg = data.Message ? data.Message : "";
			if (msg != "") {
				if (!data.Success)
					AppContext.ShowError(msg);
				else if (showSuccessMessage)
					AppContext.ShowSuccess(msg);
			}
			if (callback) callback(data);
		}
		else {
			if (callback) callback();
		}
	}

	function Error(overlay, rq, status, error) {
		if (overlay != null)
			overlay.remove();
		var msg = rq && rq.Message ? rq.Message : rq.statusText;
		AppContext.ShowError(msg);
		//if (errorhandler) errorhandler(rq, status, error);
	}

	function Complete(overlay, rq, st) {
		if (overlay != null)
			overlay.remove();
	}

	function FormDataPost(url, postdata, callback, errorhandler, showOverlay, showSuccessMessage) {
		var overlay = null;
		if (showOverlay) {
			overlay = $('<div><div id="overlay"></div><div id="overlaymessage">Processing...</div></div>');
			overlay.appendTo(document.body)
		}
		if (showSuccessMessage == null) showSuccessMessage = true;

		return $.ajax({

			url: url,
			type: 'POST',
			//contentType: " charset=utf-8",
			data: postdata,
			//dataType: 'json',

			success: function (data, status, rq) {
				Success(overlay, callback, showSuccessMessage, data, status, rq);
			},
			error: errorhandler ? errorhandler : function (rq, status, error) {
				Error(overlay, rq, status, error);
				//if (errorhandler) errorhandler(rq, status, error);
			},
			complete: function (rq, st) {
				Complete(overlay, rq, st)
			}
		});
	}

	// #endregion customer callbacks and ajax request 

	return {

		// #region

		GetNextStep: function (postdata) {
			return AppContext.HttpPost('/orders/actions/getnextstep/', postdata, null, null, true, true)
		},

		//GetPrevStep: function (orderID) {
		//	return AppContext.HttpGet(`/orders/actions/getprevstep/${orderID}`, null, null, false)
		//},

		GetStep: function (postdata) {
			return FormDataPost(`/orders/actions/getstep/`, postdata , null, null, true, true)
		},

		// return  label type articles units to validation
		GetOrdersDetails: function (postdata) {
			return AppContext.HttpPost('/order/validate/getordersdetails', postdata, null, null, true, true);
        },

        GetGroupItemsAssigned: function (postdata) {
            return AppContext.HttpPost('/order/validate/itemassignment/assigmentdetails', postdata, null, null, true, true);
        },

		SaveQuantityState: function (postdata) {
			return FormDataPost('/order/validate/savequantities', postdata, null, null, true, true)

		},

		SolveQuantities: function (postdata) {
			return FormDataPost('/order/validate/solvequantities', postdata, null, null, true, true)
		},

		// return item type articles units to validation
		GetOrderItemsDetails: function (postdata) {
			return AppContext.HttpPost('/order/validate/getordersextradetails', postdata, null, null, true, true);
        },

        GetOrderItemsDetailsUnselected: function (postdata) {
            return AppContext.HttpPost('/order/validate/getordersextradetails/unselected', postdata, null, null, true, true);
        },

		SaveExtrasState: function (postdata, asActive) {

			var q = asActive ? 1 : 0;

			return FormDataPost('/order/validate/saveextras?asActive=' + q , postdata, null, null, true, true)
		},

		SolveExtras: function (postdata) {
			return AppContext.HttpPost('/order/validate/solveextras', postdata, null, null, true, true)
        },

        SaveSupportFiles: function (postdata) {
            return FormDataPost('/supportfiles/savesupportfiles', postdata, null, null, true, true)
        },

        SolveSupportFiles: function (postdata) {
            return AppContext.HttpPost('/supportfiles/validate/solvesupportfiles', postdata, null, null, true, true)
        },

        CreateSupportFilesLog: function (postdata) {
            return AppContext.HttpPost('/supportfiles/validate/createsupportfileslog', postdata, null, null, true, true)
        },

		GetDefaultAddress: function (postdata) {
			return AppContext.HttpPost('/order/validate/getshippingaddress', postdata, null, null, true, false)
		},

		SaveShippingAddress: function (postdata) {
			return AppContext.HttpPost('/order/validate/saveshippingaddress', postdata, null, null, true, true);
		},

		SolveShippingAddress: function (postdata) {
			return AppContext.HttpPost('/order/validate/solveshippingaddress', postdata, null, null, true, true);
		},
		
		GetOrdersDetailsForReview: function (postdata) {
			return AppContext.HttpPost('/order/validate/getordersdetailsforreview', postdata, null, null, true, true);
		},

		SolveReview: function (postdata) {
			return AppContext.HttpPost('/order/validate/solvereview', postdata, null, null, true, true);
		},

        GetOrdersDetailsForReviewInditex: function (postdata) {
            return AppContext.HttpPost('/inditex/order/validate/getordersdetailsforreview', postdata, null, null, true, true);
        },

        SolveReviewInditex: function (postdata) {
            return AppContext.HttpPost('/inditex/order/validate/solvereview', postdata, null, null, true, true);
        },

		SetAsValid: function (postdata) {
			return AppContext.HttpPost('/orders/actions/setasvalidgroup', postdata, null, null, true, true);
		},
		// #endregion 

		// SolveConflict
		SolveConflict: function (postdata) {
			return AppContext.HttpPost('/order/solveconflict', postdata, null, null, true, true);
		},

		// #region composition - labellingcontroller

		GetArticlesByLabelType: function (postdata) {
			return AppContext.HttpPost(`/articlesbylabeltype/`, postdata, null, null, true, true);
		},
		
		GetOrderedLabelsGrouped: function (postdata) {
			return AppContext.HttpPost('/order/validate/getorderedlabelsgrouped', postdata, null, null, true, true);
		},

		GetAddedComposition: function (postdata) {
			return AppContext.HttpPost('/order/validate/getaddedcompo', postdata, null, null, true, true);
		},

		SaveCompositionDefined: function (postdata) {
			return FormDataPost('/order/validate/savecompodefinition', postdata, null, null, true, true);
		},

		SaveStateLabellingCompoSimple: function (postdata) {
			return AppContext.HttpPost('/order/validate/composition/savestate', postdata, null, null, true, true)

		},

		SaveAndNextLabellingCompoSimple: function (postdata) {
			return AppContext.HttpPost('/order/validate/composition/solveaddlabels', postdata, null, null, true, true)

		},

		// #endregion composition - labellingcontroller

		// #region DefineCompo

		GetCompoCatalogs: function (postdata, errorHandler, showOverlay, successMessage) {
			return AppContext.HttpPost('/order/validate/getcompocatalogs ', postdata, null, errorHandler, showOverlay, successMessage);
		},

		GetCompoPreview: function (projectId,orderId,orderGroupId,id,isLoad,fillingWeightId,fillingWeightText, exceptionsLocation, articleID, callback) {
			var url = `/order/validate/composition/getcompopreview/${projectId}/${orderId}/${orderGroupId}/${id}/${isLoad}/${fillingWeightId}/${exceptionsLocation}/${articleID}/${fillingWeightText}`
			return AppContext.HttpGet(url, callback); 
		},

		BuildCompoPreview: function (postData, callback) {
			var url = `/order/validate/composition/buildcompopreview`; 
			return AppContext.HttpPost(url, postData, callback, null, true, true);
		},
		SaveCompoPreview: function (postdata) {
			return AppContext.HttpPost('/order/validate/composition/savecompopreview', postdata, null, null, true, true);
		},
		// #endregion DefineCompo


        //#region ItemAssigment

        SaveAssigmentItems: function (postdata) {
            return AppContext.HttpPost('/order/validate/itemassignment/savearticles', postdata, null, null, true, true);
        },

        SolveAssigmentItems: function (postdata) {
            return AppContext.HttpPost('/order/validate/itemassignment/solve', postdata, null, null, true, true);
		},

		SolveAssignmentItemsBaseData: function (postdata) {
			return AppContext.HttpPost('/order/validate/itemassignment/solvebasedata', postdata, null, null, true, true);
		},

        GetOrdersDataAssigment: function (postdata) {
            return AppContext.HttpPost('/order/validate/itemassignment/getordersdetails', postdata, null, null, true, true);
        },

        SaveSizes: function (postdata) {
            return AppContext.HttpPost('/order/validate/itemassignment/SaveSizes', postdata, null, null, false, false);
        },
        SaveTagTypes: function (postdata) {
            return AppContext.HttpPost('/order/validate/itemassignment/SaveTagTypes', postdata, null, null, false, false);
        },
        UpdateTrackingCodeMaks: function (postdata) {
            return AppContext.HttpPost('/order/validate/itemassignment/UpdateTrackinCode', postdata, null, null, false, false);
        },

        //#endregion 

        // #region SetOrderArticles  CustomWizard Scalpers

        Scalpers_GetOrderData: function (postdata) {
            return AppContext.HttpPost('/order/validate/scalpers/getorderdata', postdata, null, null, true, true);
        },

        Scalpers_SaveQuantities: function (postdata) {
            return FormDataPost('/order/validate/scalpers/savequantities', postdata, null, null, true, true);
        },

        Scalpers_SolveQuantities: function (postdata) {
            //return AppContext.HttpPost('/order/validate/scalpers/solvequantities', postdata, null, null, true, true);
            return FormDataPost('/order/validate/scalpers/solvequantities', postdata, null, null, true, true);
        },

        // #endregion SetOrderArticles  CustomWizard Scalpers

		// #region CustomWizard AlvaroMoreno

        AlvaroMoreno_GetOrderData: function (postdata) {
			return AppContext.HttpPost('/order/validate/alvaromoreno/getorderdata', postdata, null, null, true, true);
		},
		AlvaroMoreno_SaveQuantities: function (postdata) {
			return FormDataPost('/order/validate/alvaromoreno/savequantities', postdata, null, null, true, true);
		},
		AlvaroMoreno_SolveQuantities: function (postdata) {
			return FormDataPost('/order/validate/alvaromoreno/solvequantities', postdata, null, null, true, true);
		}


        // #endregion CustomWizard AlvaroMoreno

	};

}();

// #endregion  Wizards


// =========================================================================
// Sage
// =========================================================================
// #region Sage
AppContext.Sage = {

	GetArticle: function (id, ref) {
		return AppContext.HttpGet(`/ws/sage/getarticle/${id}/${ref}`,null,null,true)
	},

	SyncArticle: function (id, ref) {
		return AppContext.HttpGet(`/ws/sage/syncarticle/${id}/${ref}`, null, null, true)
	},

	GetCompany: function (id, ref) {
		return AppContext.HttpGet(`/ws/sage/getcompany/${id}/${ref}`, null, null, true)
	},

	SyncCompany: function (id, ref) {
		return AppContext.HttpGet(`/ws/sage/synccompany/${id}/${ref}`, null, null, true)
	},

	GetAddress: function () {

	},

	GetOrderGroup: function () {

	},

	QueryItems: function (data) {
		return AppContext.HttpPost(`/ws/sage/queryitems/`, data, null, null, true);
	},

	ImportSelectedItems: function (data) {
		return AppContext.HttpPost(`/ws/sage/ImportItems`, data, null, null, true);
	},

	SyncArtifact: function (id, ref) {
		return AppContext.HttpGet(`/ws/sage/syncartifact/${id}/${ref}`, null, null, true)
    },

    


};

// #endregion Sage

// =========================================================================
// Certifications
// =========================================================================
// #region Certifications

AppContext.Certifications = (function () {
return {
		Insert: function (data) {
			return AppContext.HttpPost(`/companycertification/insert`,data, null, null, true)
		},
		Update: function (data) {
			return AppContext.HttpPost(`/companycertification/update`,data, null, null, true)
		},
		Delete: function (data) {
			return AppContext.HttpPost(`/companycertification/delete`,data, null, null, true)
		},
		All: function () {
			return AppContext.HttpGet(`/companycertification/all`, null, null, true)
		},
		GetByID: function (id) {
			return AppContext.HttpGet(`/companycertification/getbyid/${id}`, null, null, true)
		},
		Save: function (data) {
			return AppContext.HttpPost(`/companycertification/save`, data, null, null, true)
		},
		GetByVendorID: function (vendorID) {
			return AppContext.HttpGet(`/companycertification/getbyvendorid/${vendorID}`, null, null, true)
		},
		NewRecord: function (def) {
			var rec = {};
			for (var i = 0; i < def.length; i++) {
				rec[def[i].Name] = getDefaultValue(def[i].Type);
				if (def[i].Type == 1)
					rec[`_${def[i].Name}_DISP`] = "";
			}
			return rec;
		}
	};
	function getDefaultValue(type) {
		switch (parseInt(type, 10)) {
			case 1: return null;
			case 2: return 0;
			case 3: return 0;
			case 4: return 0;
			case 5: return false;
			case 6: return new Date();
			case 7: return "";
			case 8: return 0;
			case 9: return null;
			case 10: return 0;
			default: return null;
		}
	}
})();


// #endregion Certifications