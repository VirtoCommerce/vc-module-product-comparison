// This file is auto-generated. Do not edit manually.

import type { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
export type Maybe<T> = T;
export type InputMaybe<T> = T;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
export type MakeEmpty<T extends { [key: string]: unknown }, K extends keyof T> = { [_ in K]?: never };
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: { input: string; output: string; }
  String: { input: string; output: string; }
  Boolean: { input: boolean; output: boolean; }
  Int: { input: number; output: number; }
  Float: { input: number; output: number; }
  AnyValue: { input: any; output: any; }
  Date: { input: string; output: string; }
  DateTime: { input: any; output: any; }
  Decimal: { input: number; output: number; }
  DynamicPropertyValue: { input: string | number | boolean | null; output: string | number | boolean | null; }
  Long: { input: number; output: number; }
  ModuleSettingValue: { input: string | number | boolean | null; output: string | number | boolean | null; }
  OptionalDecimal: { input: number | undefined; output: number | undefined; }
  OptionalNullableDecimal: { input: number | null | undefined; output: number | null | undefined; }
  OptionalString: { input: string | undefined; output: string | undefined; }
  PropertyValue: { input: string | number | boolean | null; output: string | number | boolean | null; }
  Seconds: { input: number; output: number; }
  StoreAssetUrl: { input: any; output: any; }
};

export type ActivateBackInStockSubscriptionCommandType = {
  productId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
};

export type AddAddressToFavoritesCommandType = {
  addressId: Scalars['String']['input'];
};

export type AddQuoteAttachmentsCommandType = {
  quoteId: Scalars['String']['input'];
  urls: Array<InputMaybe<Scalars['String']['input']>>;
};

export type AddQuoteItemsCommandType = {
  newQuoteItems: Array<InputMaybe<InputNewQuoteItemType>>;
  quoteId: Scalars['String']['input'];
};

export type ApproveQuoteCommandType = {
  quoteId: Scalars['String']['input'];
};

export type CancelQuoteCommandType = {
  comment: Scalars['String']['input'];
  quoteId: Scalars['String']['input'];
};

export type CancelReturnCommandType = {
  /** Why the buyer withdrew the return. */
  reason?: InputMaybe<Scalars['String']['input']>;
  returnId: Scalars['String']['input'];
};

export type ChangeQuoteCommentCommandType = {
  comment: Scalars['String']['input'];
  quoteId: Scalars['String']['input'];
};

export type ChangeQuoteItemQuantityCommandType = {
  lineItemId: Scalars['String']['input'];
  quantity: Scalars['Int']['input'];
  quoteId: Scalars['String']['input'];
};

export type ConfigurableProductOptionInput = {
  /** Product ID */
  productId: Scalars['String']['input'];
  /** Quantity of product */
  quantity: Scalars['Int']['input'];
  /** Whether the configuration item is selected for checkout */
  selectedForCheckout?: InputMaybe<Scalars['Boolean']['input']>;
};

export type ConfigurableProductOptionKeyInput = {
  /** Product ID */
  productId: Scalars['String']['input'];
};

export type ConfigurationSectionInput = {
  /** Custom text for 'Text' section */
  customText?: InputMaybe<Scalars['String']['input']>;
  /** List of file links for 'File' section */
  fileUrls?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  /** Configuration section option/product */
  option?: InputMaybe<ConfigurableProductOptionInput>;
  /** Configuration section ID */
  sectionId: Scalars['String']['input'];
  /** Configuration section type. Possible values: 'Product', 'Variation', 'Text', 'File' */
  type: Scalars['String']['input'];
};

export type ConfigurationSectionKeyInput = {
  /** Identifying subset of the configuration section option (Product/Variation only) */
  option?: InputMaybe<ConfigurableProductOptionKeyInput>;
  /** Configuration section ID */
  sectionId: Scalars['String']['input'];
  /** Configuration section type. Possible values: 'Product', 'Variation', 'Text', 'File' */
  type: Scalars['String']['input'];
};

export type ConfirmTaskCommandType = {
  id: Scalars['String']['input'];
};

export type CreateQuoteCommandType = {
  cultureName: Scalars['String']['input'];
  currencyCode: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type CreateQuoteFromCartCommandType = {
  cartId: Scalars['String']['input'];
  comment: Scalars['String']['input'];
};

export type CreateReturnCommandType = {
  /** Culture the buyer is using, such as en-US. The return's status emails are written in it; without it, in the order's. */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  customerComment?: InputMaybe<Scalars['String']['input']>;
  customerReference?: InputMaybe<Scalars['String']['input']>;
  items: Array<InputReturnItemType>;
  orderId: Scalars['String']['input'];
};

export type CreateReviewCommandType = {
  entityId: Scalars['String']['input'];
  entityType: Scalars['String']['input'];
  imageUrls?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  rating: Scalars['Int']['input'];
  review: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
};

export enum CustomerReviewStatus {
  Approved = 'APPROVED',
  New = 'NEW',
  Rejected = 'REJECTED'
}

export type DeactivateBackInStockSubscriptionCommandType = {
  productId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
};

export type DeclineQuoteCommandType = {
  quoteId: Scalars['String']['input'];
};

export type DeleteFileCommandType = {
  id: Scalars['String']['input'];
};

export type DeleteQuoteAttachmentsCommandType = {
  quoteId: Scalars['String']['input'];
  urls: Array<InputMaybe<Scalars['String']['input']>>;
};

export type DeleteSkyflowCardCommandType = {
  /** Skyflow card id */
  skyflowId: Scalars['String']['input'];
  /** Store id */
  storeId: Scalars['String']['input'];
};

/** Dynamic property value type */
export enum DynamicPropertyValueTypes {
  Boolean = 'BOOLEAN',
  DateTime = 'DATE_TIME',
  Decimal = 'DECIMAL',
  Html = 'HTML',
  Image = 'IMAGE',
  Integer = 'INTEGER',
  LongText = 'LONG_TEXT',
  ShortText = 'SHORT_TEXT',
  Undefined = 'UNDEFINED'
}

export enum FacetTypes {
  Filter = 'FILTER',
  Range = 'RANGE',
  Terms = 'TERMS'
}

export type InputAcceptRejectOrganizationInviteType = {
  /** ID of the organization the current user was invited to */
  organizationId: Scalars['String']['input'];
};

export type InputAddBulkItemsType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  /** Bulk cart items */
  cartItems: Array<InputMaybe<InputNewBulkItemType>>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputAddConfigurationItemType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Configuration section to add */
  configurationSection: ConfigurationSectionInput;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item ID */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputAddConfigurationItemsType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** List of configuration sections to add */
  configurationSections: Array<ConfigurationSectionInput>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item ID */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputAddCouponType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Coupon code */
  couponCode: Scalars['String']['input'];
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputAddFcmTokenType = {
  token: Scalars['String']['input'];
};

export type InputAddGiftItemsType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** IDs of gift rewards to add to the cart */
  ids: Array<InputMaybe<Scalars['String']['input']>>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputAddItemType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Comment */
  comment?: InputMaybe<Scalars['String']['input']>;
  /** Configurable product support. List of configurable product sections */
  configurationSections?: InputMaybe<Array<InputMaybe<ConfigurationSectionInput>>>;
  /** Create date. Optional, to manually control line item position in the cart if required. ISO-8601 format, for example: 2025-01-23T11:46:11Z */
  createdDate?: InputMaybe<Scalars['DateTime']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  /** Add the product in a different currency */
  itemCurrencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Price */
  price?: InputMaybe<Scalars['Decimal']['input']>;
  /** Product ID */
  productId: Scalars['String']['input'];
  /** Quantity */
  quantity: Scalars['Int']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputAddItemsType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  /** Cart items */
  cartItems: Array<InputMaybe<InputNewCartItemType>>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputAddOrUpdateCartAddressType = {
  /** Address */
  address: InputAddressType;
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputAddOrUpdateCartPaymentType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Payment */
  payment: InputPaymentType;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputAddOrUpdateCartShipmentType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Shipment */
  shipment: InputShipmentType;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputAddOrUpdateOrderPaymentType = {
  /** Order ID */
  orderId: Scalars['String']['input'];
  /** Payment */
  payment: InputOrderPaymentType;
};

export type InputAddWishlistBulkItemType = {
  /** Configurable product support. List of configurable product sections */
  configurationSections?: InputMaybe<Array<InputMaybe<ConfigurationSectionInput>>>;
  /** Wish list ids */
  listIds: Array<InputMaybe<Scalars['String']['input']>>;
  /** Product id to add */
  productId: Scalars['String']['input'];
  /** Product quantity to add */
  quantity?: InputMaybe<Scalars['Int']['input']>;
};

export type InputAddWishlistItemType = {
  /** Configurable product support. List of configurable product sections */
  configurationSections?: InputMaybe<Array<InputMaybe<ConfigurationSectionInput>>>;
  /** Wish list id */
  listId: Scalars['String']['input'];
  /** Product id to add */
  productId: Scalars['String']['input'];
  /** Product quantity to add */
  quantity?: InputMaybe<Scalars['Int']['input']>;
};

export type InputAddWishlistItemsType = {
  listId: Scalars['String']['input'];
  /** List items */
  listItems: Array<InputNewWishlistItemType>;
};

export type InputAddressType = {
  addressType?: InputMaybe<Scalars['Int']['input']>;
  /** City */
  city?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Country code */
  countryCode?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Country */
  countryName?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Description */
  description?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Email */
  email?: InputMaybe<Scalars['OptionalString']['input']>;
  /** First name */
  firstName?: InputMaybe<Scalars['OptionalString']['input']>;
  /** ID */
  id?: InputMaybe<Scalars['OptionalString']['input']>;
  /** ID */
  key?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Last name */
  lastName?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Line1 */
  line1?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Line2 */
  line2?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Middle name */
  middleName?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Name */
  name?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Company name */
  organization?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Outer ID */
  outerId?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Phone */
  phone?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Postal code */
  postalCode?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Region ID */
  regionId?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Region */
  regionName?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Zip */
  zip?: InputMaybe<Scalars['OptionalString']['input']>;
};

export type InputApplicationUserLoginType = {
  loginProvider: Scalars['String']['input'];
  providerKey: Scalars['String']['input'];
};

export type InputAssignPermissionScopeType = {
  scope: Scalars['String']['input'];
  type: Scalars['String']['input'];
};

export type InputAssignPermissionType = {
  assignedScopes?: InputMaybe<Array<InputMaybe<InputAssignPermissionScopeType>>>;
  name: Scalars['String']['input'];
};

export type InputAssignRoleType = {
  concurrencyStamp?: InputMaybe<Scalars['String']['input']>;
  id: Scalars['String']['input'];
  name: Scalars['String']['input'];
  permissions: Array<InputMaybe<InputAssignPermissionType>>;
};

export type InputAuthorizePaymentType = {
  /** Culture name */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Order Id */
  orderId?: InputMaybe<Scalars['String']['input']>;
  /** Input parameters */
  parameters?: InputMaybe<Array<InputMaybe<InputKeyValueType>>>;
  /** Payment Id */
  paymentId: Scalars['String']['input'];
  /** Store Id */
  storeId?: InputMaybe<Scalars['String']['input']>;
};

export type InputCartItemQuantityType = {
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  /** New quantity */
  quantity: Scalars['Int']['input'];
};

export type InputChangeAllCartConfigurationItemsSelectedType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeAllCartItemsSelectedType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCartConfigurationItemSelectedType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Configuration section that identifies the configuration item to toggle */
  configurationSection: ConfigurationSectionKeyInput;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  /** Is configuration item selected for checkout */
  selectedForCheckout: Scalars['Boolean']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCartConfigurationItemsSelectedType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Configuration sections that identify the configuration items to toggle */
  configurationSections: Array<ConfigurationSectionKeyInput>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCartConfiguredItemType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Configuration sections */
  configurationSections?: InputMaybe<Array<InputMaybe<ConfigurationSectionInput>>>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  /** Quantity */
  quantity?: InputMaybe<Scalars['Int']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCartCurrencyType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Second cart currency */
  newCurrencyCode: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCartItemCommentType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Comment */
  comment: Scalars['String']['input'];
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCartItemPriceType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  /** Price */
  price: Scalars['Decimal']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCartItemQuantityType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  /** Quantity */
  quantity: Scalars['Int']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCartItemSelectedType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  /** Is item selected for checkout */
  selectedForCheckout: Scalars['Boolean']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCartItemsQuantityType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  /** Cart items */
  cartItems: Array<InputMaybe<InputCartItemQuantityType>>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCartItemsSelectedType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** List of line item Ids */
  lineItemIds?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeCommentType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Comment */
  comment?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeOrderStatusType = {
  /** Order ID */
  orderId: Scalars['String']['input'];
  /** Order status */
  status: Scalars['String']['input'];
};

export type InputChangeOrganizationContactRoleType = {
  /** Contact member ID to be changed */
  memberId: Scalars['String']['input'];
  /** Role IDs or names to be assigned to the user within the organization */
  roleIds?: InputMaybe<Array<Scalars['String']['input']>>;
  /** ID of store whose company-role whitelist should be used for validation. When omitted, the member's own store is used */
  storeId?: InputMaybe<Scalars['String']['input']>;
};

export type InputChangeOrganizationLogoCommandType = {
  logoUrl?: InputMaybe<Scalars['String']['input']>;
  organizationId: Scalars['String']['input'];
};

export type InputChangePasswordType = {
  /** New password according with system security policy */
  newPassword: Scalars['String']['input'];
  /** Old user password */
  oldPassword: Scalars['String']['input'];
  /** User identifier */
  userId: Scalars['String']['input'];
};

export type InputChangePurchaseOrderNumber = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Purchase Order Number */
  purchaseOrderNumber?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputChangeSalesRepTaskStatus = {
  /** True marks the task done; false reopens it. */
  completed: Scalars['Boolean']['input'];
  /** Id of the task to change. Must be a task the caller owns. */
  id: Scalars['String']['input'];
};

export type InputChangeWishlistType = {
  /** Ids to share the list with (id space defined by scope); ids already shared with are ignored; requires scope in the same write; ignored by scopes without targets */
  addSharedWithIds?: InputMaybe<Array<Scalars['String']['input']>>;
  /** Culture name */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** List description */
  description?: InputMaybe<Scalars['String']['input']>;
  /** List ID */
  listId: Scalars['String']['input'];
  /** New List name */
  listName?: InputMaybe<Scalars['String']['input']>;
  /** Message saved with the share (one for all targets), max 1024 characters; null leaves it unchanged, an empty or whitespace-only string clears it; requires scope in the same write; ignored by scopes without targets */
  message?: InputMaybe<Scalars['String']['input']>;
  /** Ids to stop sharing the list with; ids not shared with are ignored; requires scope in the same write; ignored by scopes without targets */
  removeSharedWithIds?: InputMaybe<Array<Scalars['String']['input']>>;
  /** List scope (private or organization) */
  scope?: InputMaybe<Scalars['String']['input']>;
  /** The single recipient the list is shared with (id space defined by scope): replaces the one it currently has, and is refused when the list has several; requires scope in the same write; ignored by scopes without targets */
  sharedWithId?: InputMaybe<Scalars['String']['input']>;
  /** Sharing key (URL argument) */
  sharingKey?: InputMaybe<Scalars['String']['input']>;
};

export type InputClearCartType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputClearCompareListCategoryType = {
  /** Category ID as the compare list returns it; null for products without a category */
  categoryId?: InputMaybe<Scalars['String']['input']>;
  /** Culture name ("en-US") for the returned products */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Currency code ("USD") for the returned products */
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Store ID */
  storeId: Scalars['String']['input'];
  /** The signed-in user's ID, or the guest's anonymous ID */
  userId: Scalars['String']['input'];
};

export type InputClearCompareListType = {
  /** Culture name ("en-US") for the returned products */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Currency code ("USD") for the returned products */
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Store ID */
  storeId: Scalars['String']['input'];
  /** The signed-in user's ID, or the guest's anonymous ID */
  userId: Scalars['String']['input'];
};

export type InputClearPaymentsType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputClearShipmentsType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputCloneWishlistType = {
  /** Ids to share the list with (id space defined by scope); ids already shared with are ignored; requires scope in the same write; ignored by scopes without targets */
  addSharedWithIds?: InputMaybe<Array<Scalars['String']['input']>>;
  /** Culture name */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Currency code */
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** List description */
  description?: InputMaybe<Scalars['String']['input']>;
  /** Source List ID */
  listId: Scalars['String']['input'];
  /** List name */
  listName?: InputMaybe<Scalars['String']['input']>;
  /** Message saved with the share (one for all targets), max 1024 characters; null leaves it unchanged, an empty or whitespace-only string clears it; requires scope in the same write; ignored by scopes without targets */
  message?: InputMaybe<Scalars['String']['input']>;
  /** Ids to stop sharing the list with; ids not shared with are ignored; requires scope in the same write; ignored by scopes without targets */
  removeSharedWithIds?: InputMaybe<Array<Scalars['String']['input']>>;
  /** List scope (private or organization) */
  scope?: InputMaybe<Scalars['String']['input']>;
  /** Sharing key (URL argument) */
  sharingKey?: InputMaybe<Scalars['String']['input']>;
  /** Store ID */
  storeId: Scalars['String']['input'];
  /** Owner ID */
  userId: Scalars['String']['input'];
};

export type InputCompareListProductType = {
  /** Category to compare the product in; empty for none */
  categoryId?: InputMaybe<Scalars['String']['input']>;
  /** The chosen configuration of a configurable product: Product and Text sections */
  configurationSections?: InputMaybe<Array<ConfigurationSectionInput>>;
  /** Product ID */
  productId: Scalars['String']['input'];
};

export type InputConfirmEmailType = {
  /** Confirm email token */
  token: Scalars['String']['input'];
  /** User identifier */
  userId: Scalars['String']['input'];
};

export type InputCreateApplicationUserType = {
  /** Username of the creator */
  createdBy?: InputMaybe<Scalars['String']['input']>;
  /** Date of user creation */
  createdDate?: InputMaybe<Scalars['DateTime']['input']>;
  /** User Email */
  email: Scalars['String']['input'];
  /** User ID */
  id?: InputMaybe<Scalars['String']['input']>;
  /** Can user be locked out */
  lockoutEnabled?: InputMaybe<Scalars['Boolean']['input']>;
  /** End date of lockout */
  lockoutEnd?: InputMaybe<Scalars['DateTime']['input']>;
  /** External logins */
  logins?: InputMaybe<Array<InputMaybe<InputApplicationUserLoginType>>>;
  /** Id of the associated Member */
  memberId?: InputMaybe<Scalars['String']['input']>;
  /** User password (nullable, for external logins) */
  password?: InputMaybe<Scalars['String']['input']>;
  /** Password expiration date */
  passwordExpired?: InputMaybe<Scalars['Boolean']['input']>;
  /** User phone number */
  phoneNumber?: InputMaybe<Scalars['String']['input']>;
  /** Is user phone number confirmed */
  phoneNumberConfirmed?: InputMaybe<Scalars['Boolean']['input']>;
  /** User photo URL */
  photoUrl?: InputMaybe<Scalars['String']['input']>;
  /** List of user roles */
  roles?: InputMaybe<Array<InputMaybe<InputAssignRoleType>>>;
  /** Associated Store Id */
  storeId?: InputMaybe<Scalars['String']['input']>;
  /** Is Two Factor Authentication enabled */
  twoFactorEnabled?: InputMaybe<Scalars['Boolean']['input']>;
  /** User name */
  userName: Scalars['String']['input'];
  /** User type (Manager, Customer) */
  userType: Scalars['String']['input'];
};

export type InputCreateCartFromWishlistType = {
  /** Wishlist ID */
  listId: Scalars['String']['input'];
};

export type InputCreateConfiguredLineItemCommand = {
  configurableProductId: Scalars['String']['input'];
  configurationSections?: InputMaybe<Array<InputMaybe<ConfigurationSectionInput>>>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId?: InputMaybe<Scalars['String']['input']>;
};

export type InputCreateContactType = {
  about?: InputMaybe<Scalars['String']['input']>;
  addresses?: InputMaybe<Array<InputMaybe<InputMemberAddressType>>>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  defaultLanguage?: InputMaybe<Scalars['String']['input']>;
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  emails?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  firstName: Scalars['String']['input'];
  fullName?: InputMaybe<Scalars['String']['input']>;
  groups?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  id?: InputMaybe<Scalars['String']['input']>;
  lastName: Scalars['String']['input'];
  memberType?: InputMaybe<Scalars['String']['input']>;
  middleName?: InputMaybe<Scalars['String']['input']>;
  name?: InputMaybe<Scalars['String']['input']>;
  organizations?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  phones?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  photoUrl?: InputMaybe<Scalars['String']['input']>;
  salutation?: InputMaybe<Scalars['String']['input']>;
  selectedAddressId?: InputMaybe<Scalars['String']['input']>;
  timeZone?: InputMaybe<Scalars['String']['input']>;
};

export type InputCreateOrderFromCartType = {
  /** Cart ID */
  cartId?: InputMaybe<Scalars['String']['input']>;
};

export type InputCreateOrganizationType = {
  addresses?: InputMaybe<Array<InputMaybe<InputMemberAddressType>>>;
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  name?: InputMaybe<Scalars['String']['input']>;
};

export type InputCreatePasskeyType = {
  credential: Scalars['String']['input'];
  name?: InputMaybe<Scalars['String']['input']>;
};

export type InputCreateSalesRepTask = {
  /** Organization id of a customer the caller serves - the customer the task is about. Null or empty means none. */
  customerId?: InputMaybe<Scalars['String']['input']>;
  /** Free-text notes. Null or empty clears it. */
  description?: InputMaybe<Scalars['String']['input']>;
  /** When the task is due. Required on create; null on update clears it. */
  dueDate?: InputMaybe<Scalars['DateTime']['input']>;
  /** Task title (required, max 256 chars). */
  name: Scalars['String']['input'];
  /** Lowest, Low, Normal, High or Highest. Null or empty means Normal. */
  priority?: InputMaybe<Scalars['String']['input']>;
  /** Free text, max 128 chars, typically one of the values salesRepTaskTypes offers - not enforced. Null or empty clears it. */
  type?: InputMaybe<Scalars['String']['input']>;
};

export type InputCreateUserType = {
  /** Application user to create */
  applicationUser: InputCreateApplicationUserType;
};

export type InputCreateWishlistType = {
  /** Ids to share the list with (id space defined by scope); ids already shared with are ignored; requires scope in the same write; ignored by scopes without targets */
  addSharedWithIds?: InputMaybe<Array<Scalars['String']['input']>>;
  /** Culture name */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Currency code */
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** List description */
  description?: InputMaybe<Scalars['String']['input']>;
  /** List name */
  listName?: InputMaybe<Scalars['String']['input']>;
  /** Message saved with the share (one for all targets), max 1024 characters; null leaves it unchanged, an empty or whitespace-only string clears it; requires scope in the same write; ignored by scopes without targets */
  message?: InputMaybe<Scalars['String']['input']>;
  /** Ids to stop sharing the list with; ids not shared with are ignored; requires scope in the same write; ignored by scopes without targets */
  removeSharedWithIds?: InputMaybe<Array<Scalars['String']['input']>>;
  /** List scope (private or organization) */
  scope?: InputMaybe<Scalars['String']['input']>;
  /** The single recipient the list is shared with (id space defined by scope): replaces the one it currently has, and is refused when the list has several; requires scope in the same write; ignored by scopes without targets */
  sharedWithId?: InputMaybe<Scalars['String']['input']>;
  /** Sharing key (URL argument) */
  sharingKey?: InputMaybe<Scalars['String']['input']>;
  /** Store ID */
  storeId: Scalars['String']['input'];
  /** Owner ID */
  userId: Scalars['String']['input'];
};

export type InputDeleteContactType = {
  contactId: Scalars['String']['input'];
};

export type InputDeleteFcmTokenType = {
  token: Scalars['String']['input'];
};

export type InputDeleteMemberAddressType = {
  addresses: Array<InputMaybe<InputMemberAddressType>>;
  memberId: Scalars['String']['input'];
};

export type InputDeleteSalesRepTask = {
  /** Id of the task to delete. Must be a task the caller owns. */
  id: Scalars['String']['input'];
};

export type InputDeleteSignInKeyType = {
  keyId: Scalars['String']['input'];
  signInMethodName: Scalars['String']['input'];
};

export type InputDeleteUserType = {
  userNames: Array<InputMaybe<Scalars['String']['input']>>;
};

export type InputDynamicPropertyValueType = {
  /** Culture name ("en-US") for multilingual property */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Dynamic property name */
  name: Scalars['String']['input'];
  /** Dynamic property value. ID must be passed for dictionary item */
  value?: InputMaybe<Scalars['DynamicPropertyValue']['input']>;
};

export type InputInitializeCartPaymentType = {
  cartId: Scalars['String']['input'];
  /** Culture name */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Payment Id */
  paymentId: Scalars['String']['input'];
  /** Store Id */
  storeId?: InputMaybe<Scalars['String']['input']>;
};

export type InputInitializePaymentType = {
  /** Culture name */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Order Id */
  orderId?: InputMaybe<Scalars['String']['input']>;
  /** Input parameters */
  parameters?: InputMaybe<Array<InputMaybe<InputKeyValueType>>>;
  /** Payment Id */
  paymentId: Scalars['String']['input'];
  /** Store Id */
  storeId?: InputMaybe<Scalars['String']['input']>;
};

export type InputInviteUserType = {
  /** Customer order Id to be associated with this user. */
  customerOrderId?: InputMaybe<Scalars['String']['input']>;
  /** Emails which will receive invites */
  emails: Array<Scalars['String']['input']>;
  /** Optional message to include into email with instructions which invites persons will see */
  message?: InputMaybe<Scalars['String']['input']>;
  /** ID of organization where contact will be added for user */
  organizationId?: InputMaybe<Scalars['String']['input']>;
  /** Role IDs or names to be assigned to the invited user */
  roleIds?: InputMaybe<Array<Scalars['String']['input']>>;
  /** ID of store which will send invites */
  storeId: Scalars['String']['input'];
  /** Optional URL suffix: you may provide here relative URL to your page which handle registration by invite */
  urlSuffix?: InputMaybe<Scalars['String']['input']>;
};

export type InputKeyValueType = {
  /** Dictionary key */
  key: Scalars['String']['input'];
  /** Dictionary value */
  value?: InputMaybe<Scalars['String']['input']>;
};

export type InputLockUnlockOrganizationContactType = {
  /** Contact member ID */
  memberId: Scalars['String']['input'];
};

export type InputMarkPushMessageReadType = {
  messageId: Scalars['String']['input'];
};

export type InputMarkPushMessageUnreadType = {
  messageId: Scalars['String']['input'];
};

export type InputMemberAddressType = {
  addressType?: InputMaybe<Scalars['Int']['input']>;
  /** City */
  city: Scalars['String']['input'];
  /** Country code */
  countryCode: Scalars['String']['input'];
  /** Country name */
  countryName?: InputMaybe<Scalars['String']['input']>;
  /** Description */
  description?: InputMaybe<Scalars['String']['input']>;
  /** Email */
  email?: InputMaybe<Scalars['String']['input']>;
  /** First name */
  firstName?: InputMaybe<Scalars['String']['input']>;
  /** Id */
  id?: InputMaybe<Scalars['String']['input']>;
  /** key */
  key?: InputMaybe<Scalars['String']['input']>;
  /** Last name */
  lastName?: InputMaybe<Scalars['String']['input']>;
  /** Line1 */
  line1: Scalars['String']['input'];
  /** Line2 */
  line2?: InputMaybe<Scalars['String']['input']>;
  /** Middle name */
  middleName?: InputMaybe<Scalars['String']['input']>;
  /** Name */
  name?: InputMaybe<Scalars['String']['input']>;
  /** Company name */
  organization?: InputMaybe<Scalars['String']['input']>;
  /** Outer id */
  outerId?: InputMaybe<Scalars['String']['input']>;
  /** Phone */
  phone?: InputMaybe<Scalars['String']['input']>;
  /** Postal code */
  postalCode: Scalars['String']['input'];
  /** Region id */
  regionId?: InputMaybe<Scalars['String']['input']>;
  /** Region name */
  regionName?: InputMaybe<Scalars['String']['input']>;
  /** Zip */
  zip?: InputMaybe<Scalars['String']['input']>;
};

export type InputMergeCartType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Delete second cart after merge */
  deleteAfterMerge?: InputMaybe<Scalars['Boolean']['input']>;
  /** Second cart Id */
  secondCartId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputMergeCompareListType = {
  /** Culture name ("en-US") for the returned products */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Currency code ("USD") for the returned products */
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** ID of the guest's compare list to merge into this one */
  secondListId: Scalars['String']['input'];
  /** Store ID */
  storeId: Scalars['String']['input'];
  /** The signed-in user's ID, or the guest's anonymous ID */
  userId: Scalars['String']['input'];
};

export type InputMoveCompareListItemType = {
  /** Culture name ("en-US") for the returned products */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Currency code ("USD") for the returned products */
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** ID of the compare list item to move */
  itemId: Scalars['String']['input'];
  /** New position within the item's category, from 0 */
  position: Scalars['Int']['input'];
  /** Store ID */
  storeId: Scalars['String']['input'];
  /** The signed-in user's ID, or the guest's anonymous ID */
  userId: Scalars['String']['input'];
};

export type InputMoveWishlistItemType = {
  /** Destination List ID */
  destinationListId: Scalars['String']['input'];
  /** Line item ID to move */
  lineItemId: Scalars['String']['input'];
  /** Source List ID */
  listId: Scalars['String']['input'];
};

export type InputNewBulkItemType = {
  /** Product SKU */
  productSku: Scalars['String']['input'];
  /** Product quantity */
  quantity?: InputMaybe<Scalars['Int']['input']>;
};

export type InputNewCartItemType = {
  /** Comment */
  comment?: InputMaybe<Scalars['String']['input']>;
  /** Configurable product sections */
  configurationSections?: InputMaybe<Array<InputMaybe<ConfigurationSectionInput>>>;
  /** Line item created date override */
  createdDate?: InputMaybe<Scalars['DateTime']['input']>;
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  /** Add the product in a different currency */
  itemCurrencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Price */
  price?: InputMaybe<Scalars['Decimal']['input']>;
  /** Product Id */
  productId: Scalars['String']['input'];
  /** Product quantity */
  quantity?: InputMaybe<Scalars['Int']['input']>;
};

export type InputNewQuoteItemType = {
  comment?: InputMaybe<Scalars['String']['input']>;
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  name?: InputMaybe<Scalars['String']['input']>;
  price?: InputMaybe<Scalars['Decimal']['input']>;
  productId?: InputMaybe<Scalars['String']['input']>;
  quantity: Scalars['Int']['input'];
};

export type InputNewWishlistItemType = {
  /** Product Id */
  productId: Scalars['String']['input'];
  /** Product quantity */
  quantity?: InputMaybe<Scalars['Int']['input']>;
};

export type InputOrderAddressType = {
  addressType?: InputMaybe<Scalars['Int']['input']>;
  /** City */
  city?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Country code */
  countryCode?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Country name */
  countryName?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Email */
  email?: InputMaybe<Scalars['OptionalString']['input']>;
  /** First name */
  firstName?: InputMaybe<Scalars['OptionalString']['input']>;
  /** ID */
  id?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Id */
  key?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Last name */
  lastName?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Line1 */
  line1?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Line2 */
  line2?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Middle name */
  middleName?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Name */
  name?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Company name */
  organization?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Outer id */
  outerId?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Phone */
  phone?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Postal code */
  postalCode?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Region id */
  regionId?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Region name */
  regionName?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Zip */
  zip?: InputMaybe<Scalars['OptionalString']['input']>;
};

export type InputOrderPaymentType = {
  amount?: InputMaybe<Scalars['OptionalDecimal']['input']>;
  billingAddress?: InputMaybe<InputOrderAddressType>;
  /** Text comment */
  comment?: InputMaybe<Scalars['OptionalString']['input']>;
  currency?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Dynamic properties */
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  /** Payment ID */
  id?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Payment outer ID value */
  outerId?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Payment gateway code value */
  paymentGatewayCode?: InputMaybe<Scalars['OptionalString']['input']>;
  price?: InputMaybe<Scalars['OptionalDecimal']['input']>;
  /** Payment vendor ID value */
  vendorId?: InputMaybe<Scalars['OptionalString']['input']>;
};

export type InputPaymentType = {
  amount?: InputMaybe<Scalars['OptionalDecimal']['input']>;
  billingAddress?: InputMaybe<InputAddressType>;
  /** Text comment */
  comment?: InputMaybe<Scalars['OptionalString']['input']>;
  currency?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Dynamic properties */
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  /** Payment ID */
  id?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Payment outer ID value */
  outerId?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Payment gateway code value */
  paymentGatewayCode?: InputMaybe<Scalars['OptionalString']['input']>;
  price?: InputMaybe<Scalars['OptionalDecimal']['input']>;
  purpose?: InputMaybe<Scalars['OptionalString']['input']>;
  vendorId?: InputMaybe<Scalars['OptionalString']['input']>;
};

export type InputPersonalDataType = {
  email?: InputMaybe<Scalars['String']['input']>;
  firstName?: InputMaybe<Scalars['String']['input']>;
  fullName?: InputMaybe<Scalars['String']['input']>;
  lastName?: InputMaybe<Scalars['String']['input']>;
  middleName?: InputMaybe<Scalars['String']['input']>;
};

export type InputPushHistoricalEventType = {
  eventType?: InputMaybe<Scalars['String']['input']>;
  productId?: InputMaybe<Scalars['String']['input']>;
  productIds?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  sessionId?: InputMaybe<Scalars['String']['input']>;
  storeId?: InputMaybe<Scalars['String']['input']>;
};

export type InputQuoteAddressType = {
  addressType?: InputMaybe<Scalars['Int']['input']>;
  city: Scalars['String']['input'];
  countryCode?: InputMaybe<Scalars['String']['input']>;
  countryName: Scalars['String']['input'];
  email?: InputMaybe<Scalars['String']['input']>;
  firstName?: InputMaybe<Scalars['String']['input']>;
  id?: InputMaybe<Scalars['String']['input']>;
  key?: InputMaybe<Scalars['String']['input']>;
  lastName?: InputMaybe<Scalars['String']['input']>;
  line1?: InputMaybe<Scalars['String']['input']>;
  line2?: InputMaybe<Scalars['String']['input']>;
  name?: InputMaybe<Scalars['String']['input']>;
  /** Company name */
  organization?: InputMaybe<Scalars['String']['input']>;
  outerId?: InputMaybe<Scalars['String']['input']>;
  phone?: InputMaybe<Scalars['String']['input']>;
  postalCode?: InputMaybe<Scalars['String']['input']>;
  regionId?: InputMaybe<Scalars['String']['input']>;
  regionName?: InputMaybe<Scalars['String']['input']>;
};

export type InputRegisterAccountType = {
  email: Scalars['String']['input'];
  password: Scalars['String']['input'];
  username: Scalars['String']['input'];
};

export type InputRegisterByInvitationType = {
  /** Customer order Id to be associated with this user. */
  customerOrderId?: InputMaybe<Scalars['String']['input']>;
  /** First name of person */
  firstName: Scalars['String']['input'];
  /** Last name of person */
  lastName: Scalars['String']['input'];
  /** ID of the organization this invite was for. Only this organization's pending invite is approved on registration. */
  organizationId?: InputMaybe<Scalars['String']['input']>;
  /** Password */
  password: Scalars['String']['input'];
  /** Phone */
  phone?: InputMaybe<Scalars['String']['input']>;
  /** Invitation token */
  token: Scalars['String']['input'];
  /** ID of use created for invited email */
  userId: Scalars['String']['input'];
  /** Username */
  username: Scalars['String']['input'];
};

export type InputRegisterContactType = {
  about?: InputMaybe<Scalars['String']['input']>;
  address?: InputMaybe<InputMemberAddressType>;
  birthdate?: InputMaybe<Scalars['Date']['input']>;
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  firstName: Scalars['String']['input'];
  lastName: Scalars['String']['input'];
  middleName?: InputMaybe<Scalars['String']['input']>;
  phoneNumber?: InputMaybe<Scalars['String']['input']>;
};

export type InputRegisterOrganizationType = {
  address?: InputMaybe<InputMemberAddressType>;
  addresses?: InputMaybe<Array<InputMaybe<InputMemberAddressType>>>;
  description?: InputMaybe<Scalars['String']['input']>;
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  name: Scalars['String']['input'];
  phoneNumber?: InputMaybe<Scalars['String']['input']>;
};

export type InputRejectGiftItemsType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Ids of gift lineItems to reject from cart */
  ids: Array<InputMaybe<Scalars['String']['input']>>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputRemoveCartAddressType = {
  /** Address Id */
  addressId: Scalars['String']['input'];
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputRemoveCartType = {
  /** Cart Id */
  cartId: Scalars['String']['input'];
  /** User Id */
  userId: Scalars['String']['input'];
};

export type InputRemoveCompareListItemsType = {
  /** Culture name ("en-US") for the returned products */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Currency code ("USD") for the returned products */
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** IDs of the compare list items to remove */
  itemIds: Array<Scalars['String']['input']>;
  /** Store ID */
  storeId: Scalars['String']['input'];
  /** The signed-in user's ID, or the guest's anonymous ID */
  userId: Scalars['String']['input'];
};

export type InputRemoveConfigurationItemType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Configuration section to remove */
  configurationSection: ConfigurationSectionInput;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item ID */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputRemoveConfigurationItemsType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** List of configuration sections to remove */
  configurationSections: Array<ConfigurationSectionInput>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item ID */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputRemoveCouponType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Coupon code */
  couponCode?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputRemoveItemType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputRemoveItemsType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Array of line item Id */
  lineItemIds: Array<InputMaybe<Scalars['String']['input']>>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputRemoveMemberFromOrganizationType = {
  contactId?: InputMaybe<Scalars['String']['input']>;
  organizationId?: InputMaybe<Scalars['String']['input']>;
};

export type InputRemoveShipmentType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Shipment Id */
  shipmentId?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputRemoveWishlistItemType = {
  /** Line item ID to remove */
  lineItemId?: InputMaybe<Scalars['String']['input']>;
  /** List ID */
  listId: Scalars['String']['input'];
  /** Line item product ID to remove */
  productId?: InputMaybe<Scalars['String']['input']>;
};

export type InputRemoveWishlistItemsType = {
  /** Line item IDs to remove */
  lineItemIds: Array<Scalars['String']['input']>;
  /** List ID */
  listId: Scalars['String']['input'];
};

export type InputRemoveWishlistType = {
  /** List ID */
  listId: Scalars['String']['input'];
};

export type InputRenameSignInKeyType = {
  keyId: Scalars['String']['input'];
  name?: InputMaybe<Scalars['String']['input']>;
  signInMethodName: Scalars['String']['input'];
};

export type InputRequestRegistrationType = {
  /** Creating contact's account */
  account: InputRegisterAccountType;
  /** Creating contact */
  contact: InputRegisterContactType;
  /** Notification language code */
  languageCode?: InputMaybe<Scalars['String']['input']>;
  /** company type */
  organization?: InputMaybe<InputRegisterOrganizationType>;
  /** Store ID */
  storeId: Scalars['String']['input'];
};

export type InputResendOrganizationInviteType = {
  /** Contact member ID */
  memberId: Scalars['String']['input'];
  /** Optional message to include into the invitation email */
  message?: InputMaybe<Scalars['String']['input']>;
  /** Optional URL suffix: relative URL to the page which handles registration by invite */
  urlSuffix?: InputMaybe<Scalars['String']['input']>;
};

export type InputResetPasswordByTokenType = {
  /** New password according with system security policy */
  newPassword: Scalars['String']['input'];
  /** User password reset token */
  token: Scalars['String']['input'];
  /** User identifier */
  userId: Scalars['String']['input'];
};

export type InputReturnItemType = {
  /** Files already uploaded into the return attachments scope. Omit to leave the line's files alone; pass a list to make them match it exactly. */
  attachmentUrls?: InputMaybe<Array<Scalars['String']['input']>>;
  orderLineItemId: Scalars['String']['input'];
  quantity: Scalars['Int']['input'];
  reasonCode?: InputMaybe<Scalars['String']['input']>;
  reasonComment?: InputMaybe<Scalars['String']['input']>;
  serialNumber?: InputMaybe<Scalars['String']['input']>;
};

export type InputRevokeOrganizationInviteType = {
  /** Contact member ID */
  memberId: Scalars['String']['input'];
};

export type InputSalesRepLayout = {
  /** Top-level fixed regions with their blocks. */
  regions: Array<InputSalesRepLayoutRegion>;
  /** Document schema version. */
  schemaVersion: Scalars['Int']['input'];
  /** Layout surface identifier (e.g. "dashboard", "customerProfile"). */
  scope: Scalars['String']['input'];
  /** Optional store to scope the layout to. */
  storeId?: InputMaybe<Scalars['String']['input']>;
};

export type InputSalesRepLayoutBlock = {
  /** Whether the block is parked in the hidden tray. */
  hidden: Scalars['Boolean']['input'];
  /** Instance id (frontend-generated, stable across saves, unique within the layout). */
  id: Scalars['String']['input'];
  /** Block-type-specific settings (send an empty list for none). */
  settings: Array<InputSalesRepLayoutSetting>;
  /** Block type discriminator (frontend-owned vocabulary). */
  type: Scalars['String']['input'];
};

export type InputSalesRepLayoutRegion = {
  /** Blocks in render order (array position is the order). */
  blocks: Array<InputSalesRepLayoutBlock>;
  /** Fixed region id (e.g. "statistics", "mainLeft", "mainRight"). */
  id: Scalars['String']['input'];
};

export type InputSalesRepLayoutSetting = {
  /** Setting key (block-type-specific, frontend-owned vocabulary). */
  key: Scalars['String']['input'];
  /** Scalar setting value (string, number, boolean). */
  value?: InputMaybe<Scalars['AnyValue']['input']>;
};

export type InputSaveForLaterType = {
  /** Source Cart ID */
  cartId: Scalars['String']['input'];
  /** Culture name */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Currency code */
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item IDs to save for later */
  lineItemIds: Array<InputMaybe<Scalars['String']['input']>>;
  /** Store ID */
  storeId: Scalars['String']['input'];
  /** Owner ID */
  userId: Scalars['String']['input'];
};

export type InputSaveProductsToCompareListType = {
  /** Culture name ("en-US") for the returned products */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** Currency code ("USD") for the returned products */
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Products to compare; a product already in the list with the same configuration stays where it is */
  products: Array<InputCompareListProductType>;
  /** Store ID */
  storeId: Scalars['String']['input'];
  /** The signed-in user's ID, or the guest's anonymous ID */
  userId: Scalars['String']['input'];
};

export type InputSaveSearchQueryType = {
  query: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
};

export type InputSendCustomerCommunicationType = {
  /** Optional culture for localizing the email template (e.g. "en-US"). */
  cultureName?: InputMaybe<Scalars['String']['input']>;
  /** The Rep's message (required, max 1000 chars). May contain a URL. */
  message: Scalars['String']['input'];
  /** Customer organization whose members receive the message. */
  organizationId?: InputMaybe<Scalars['String']['input']>;
  /** Customer organizations whose members receive the message (each member once); at least one organization is required, at most 1000. */
  organizationIds?: InputMaybe<Array<Scalars['String']['input']>>;
  /** Send an email to the recipients. */
  sendEmail: Scalars['Boolean']['input'];
  /** Send an in-store push notification to the recipients. */
  sendPush: Scalars['Boolean']['input'];
  /** Store the message is sent on behalf of (scopes the email template and sender address). */
  storeId: Scalars['String']['input'];
  /** Optional message title/heading. */
  title?: InputMaybe<Scalars['String']['input']>;
};

export type InputSendVerifyEmailType = {
  email?: InputMaybe<Scalars['String']['input']>;
  /** Notification language code */
  languageCode?: InputMaybe<Scalars['String']['input']>;
  /** Store ID */
  storeId: Scalars['String']['input'];
  userId?: InputMaybe<Scalars['String']['input']>;
};

export type InputShipmentType = {
  /** Text comment */
  comment?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Currency value */
  currency?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Delivery address */
  deliveryAddress?: InputMaybe<InputAddressType>;
  /** Dynamic properties */
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  /** Fulfillment center iD */
  fulfillmentCenterId?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Height value */
  height?: InputMaybe<Scalars['OptionalNullableDecimal']['input']>;
  /** Shipment ID */
  id?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Length value */
  length?: InputMaybe<Scalars['OptionalNullableDecimal']['input']>;
  /** Measurement unit value */
  measureUnit?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Pickup location Id when shipment is Pickup */
  pickupLocationId?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Price value */
  price?: InputMaybe<Scalars['OptionalDecimal']['input']>;
  /** Shipping method code */
  shipmentMethodCode?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Shipping method option */
  shipmentMethodOption?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Vendor ID */
  vendorId?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Volumetric weight value */
  volumetricWeight?: InputMaybe<Scalars['OptionalNullableDecimal']['input']>;
  /** Weight value */
  weight?: InputMaybe<Scalars['OptionalNullableDecimal']['input']>;
  /** Weight unit value */
  weightUnit?: InputMaybe<Scalars['OptionalString']['input']>;
  /** Width value */
  width?: InputMaybe<Scalars['OptionalNullableDecimal']['input']>;
};

export type InputUpdateApplicationUserType = {
  /** Failed login attempts for the current user */
  accessFailedCount?: InputMaybe<Scalars['Int']['input']>;
  /** User Email */
  email: Scalars['String']['input'];
  /** User ID */
  id: Scalars['String']['input'];
  /** Can user be locked out */
  lockoutEnabled?: InputMaybe<Scalars['Boolean']['input']>;
  /** End date of lockout */
  lockoutEnd?: InputMaybe<Scalars['DateTime']['input']>;
  /** Id of the associated Member */
  memberId?: InputMaybe<Scalars['String']['input']>;
  /** Password Hash */
  passwordHash?: InputMaybe<Scalars['String']['input']>;
  /** User phone number */
  phoneNumber?: InputMaybe<Scalars['String']['input']>;
  /** Is user phone number confirmed */
  phoneNumberConfirmed?: InputMaybe<Scalars['Boolean']['input']>;
  /** User photo URL */
  photoUrl?: InputMaybe<Scalars['String']['input']>;
  /** List of user roles */
  roles?: InputMaybe<Array<InputMaybe<InputAssignRoleType>>>;
  /** SecurityStamp */
  securityStamp: Scalars['String']['input'];
  /** Associated Store Id */
  storeId?: InputMaybe<Scalars['String']['input']>;
  /** Is Two Factor Authentication enabled */
  twoFactorEnabled?: InputMaybe<Scalars['Boolean']['input']>;
  /** User name */
  userName: Scalars['String']['input'];
  /** User type (Manager, Customer) */
  userType: Scalars['String']['input'];
};

export type InputUpdateCartDynamicPropertiesType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Dynamic properties */
  dynamicProperties: Array<InputMaybe<InputDynamicPropertyValueType>>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputUpdateCartItemDynamicPropertiesType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Dynamic properties */
  dynamicProperties: Array<InputMaybe<InputDynamicPropertyValueType>>;
  /** Line item Id */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputUpdateCartPaymentDynamicPropertiesType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Dynamic properties */
  dynamicProperties: Array<InputMaybe<InputDynamicPropertyValueType>>;
  /** Payment Id */
  paymentId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputUpdateCartQuantity = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  items?: InputMaybe<Array<InputMaybe<InputUpdateCartQuantityItem>>>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputUpdateCartQuantityItem = {
  /** Add the product in a different currency */
  itemCurrencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Product ID */
  productId: Scalars['String']['input'];
  /** Quantity */
  quantity: Scalars['Int']['input'];
};

export type InputUpdateCartShipmentDynamicPropertiesType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Dynamic properties */
  dynamicProperties: Array<InputMaybe<InputDynamicPropertyValueType>>;
  /** Shipment Id */
  shipmentId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputUpdateConfigurationItemType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** Configuration section with updated quantity */
  configurationSection: ConfigurationSectionInput;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item ID */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputUpdateConfigurationItemsType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  /** List of configuration sections to update */
  configurationSections: Array<ConfigurationSectionInput>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  /** Line item ID */
  lineItemId: Scalars['String']['input'];
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type InputUpdateContactType = {
  about?: InputMaybe<Scalars['String']['input']>;
  addresses?: InputMaybe<Array<InputMaybe<InputMemberAddressType>>>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  defaultLanguage?: InputMaybe<Scalars['String']['input']>;
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  emails?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  firstName: Scalars['String']['input'];
  fullName?: InputMaybe<Scalars['String']['input']>;
  groups?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  id: Scalars['String']['input'];
  lastName: Scalars['String']['input'];
  memberType?: InputMaybe<Scalars['String']['input']>;
  middleName?: InputMaybe<Scalars['String']['input']>;
  name?: InputMaybe<Scalars['String']['input']>;
  organizations?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  phones?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  photoUrl?: InputMaybe<Scalars['String']['input']>;
  salutation?: InputMaybe<Scalars['String']['input']>;
  selectedAddressId?: InputMaybe<Scalars['String']['input']>;
  timeZone?: InputMaybe<Scalars['String']['input']>;
};

export type InputUpdateMemberAddressType = {
  addresses: Array<InputMaybe<InputMemberAddressType>>;
  memberId: Scalars['String']['input'];
};

export type InputUpdateMemberDynamicPropertiesType = {
  dynamicProperties: Array<InputMaybe<InputDynamicPropertyValueType>>;
  memberId: Scalars['String']['input'];
};

export type InputUpdateOrderDynamicPropertiesType = {
  dynamicProperties: Array<InputMaybe<InputDynamicPropertyValueType>>;
  orderId?: InputMaybe<Scalars['String']['input']>;
};

export type InputUpdateOrderItemDynamicPropertiesType = {
  dynamicProperties: Array<InputMaybe<InputDynamicPropertyValueType>>;
  lineItemId?: InputMaybe<Scalars['String']['input']>;
  orderId?: InputMaybe<Scalars['String']['input']>;
};

export type InputUpdateOrderPaymentDynamicPropertiesType = {
  dynamicProperties: Array<InputMaybe<InputDynamicPropertyValueType>>;
  orderId?: InputMaybe<Scalars['String']['input']>;
  paymentId?: InputMaybe<Scalars['String']['input']>;
};

export type InputUpdateOrderShipmentDynamicPropertiesType = {
  dynamicProperties: Array<InputMaybe<InputDynamicPropertyValueType>>;
  orderId?: InputMaybe<Scalars['String']['input']>;
  shipmentId?: InputMaybe<Scalars['String']['input']>;
};

export type InputUpdateOrganizationType = {
  addresses?: InputMaybe<Array<InputMaybe<InputMemberAddressType>>>;
  dynamicProperties?: InputMaybe<Array<InputMaybe<InputDynamicPropertyValueType>>>;
  emails?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  groups?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  id: Scalars['String']['input'];
  memberType?: InputMaybe<Scalars['String']['input']>;
  name?: InputMaybe<Scalars['String']['input']>;
  phones?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
};

export type InputUpdatePersonalDataType = {
  personalData: InputPersonalDataType;
};

export type InputUpdateRoleInnerType = {
  /** Concurrency Stamp */
  concurrencyStamp?: InputMaybe<Scalars['String']['input']>;
  /** Role description */
  description?: InputMaybe<Scalars['String']['input']>;
  /** Role ID */
  id: Scalars['String']['input'];
  /** Role name */
  name: Scalars['String']['input'];
  /** List of Role permissions */
  permissions: Array<InputMaybe<InputAssignPermissionType>>;
};

export type InputUpdateRoleType = {
  /** Role to update */
  role: InputUpdateRoleInnerType;
};

export type InputUpdateSalesRepTask = {
  /** Organization id of a customer the caller serves. Null or empty removes the link, except one to a customer the caller no longer serves, which reads back as null and is kept. A task linked to another kind of record keeps that link and cannot take a customer. */
  customerId?: InputMaybe<Scalars['String']['input']>;
  /** Free-text notes. Null or empty clears it. */
  description?: InputMaybe<Scalars['String']['input']>;
  /** When the task is due. Required on create; null on update clears it. */
  dueDate?: InputMaybe<Scalars['DateTime']['input']>;
  /** Id of the task to change. Must be a task the caller owns. */
  id: Scalars['String']['input'];
  /** Task title (required, max 256 chars). */
  name: Scalars['String']['input'];
  /** Lowest, Low, Normal, High or Highest. Null or empty means Normal. */
  priority?: InputMaybe<Scalars['String']['input']>;
  /** Free text, max 128 chars, typically one of the values salesRepTaskTypes offers - not enforced. Null or empty clears it. */
  type?: InputMaybe<Scalars['String']['input']>;
};

export type InputUpdateUserType = {
  /** Application user to update */
  applicationUser: InputUpdateApplicationUserType;
};

export type InputUpdateWishlistItemsType = {
  /** Bulk wishlist items */
  items: Array<InputMaybe<InputUpdateWishlistLineItemType>>;
  /** Wish list id */
  listId: Scalars['String']['input'];
};

export type InputUpdateWishlistLineItemType = {
  /** Line Item Id to update */
  lineItemId: Scalars['String']['input'];
  /** Product quantity to add */
  quantity: Scalars['Int']['input'];
};

export enum ProductPickupAvailabilityType {
  /** Available via global transfer (within weeks) */
  GlobalTransfer = 'GlobalTransfer',
  /** Available today (within hours) */
  Today = 'Today',
  /** Available via transfer (within days) */
  Transfer = 'Transfer'
}

/** The type of catalog property. */
export enum PropertyType {
  Catalog = 'CATALOG',
  Category = 'CATEGORY',
  Product = 'PRODUCT',
  Variation = 'VARIATION'
}

/** The type of catalog property value. */
export enum PropertyValueTypes {
  Boolean = 'BOOLEAN',
  Color = 'COLOR',
  DateTime = 'DATE_TIME',
  GeoPoint = 'GEO_POINT',
  Html = 'HTML',
  Integer = 'INTEGER',
  LongText = 'LONG_TEXT',
  Measure = 'MEASURE',
  Number = 'NUMBER',
  ShortText = 'SHORT_TEXT'
}

export type RefreshCartType = {
  cartId?: InputMaybe<Scalars['String']['input']>;
  cartName?: InputMaybe<Scalars['String']['input']>;
  cartType?: InputMaybe<Scalars['String']['input']>;
  cultureName?: InputMaybe<Scalars['String']['input']>;
  currencyCode?: InputMaybe<Scalars['String']['input']>;
  storeId: Scalars['String']['input'];
  userId: Scalars['String']['input'];
};

export type RejectTaskCommandType = {
  id: Scalars['String']['input'];
};

export type RemoveAddressFromFavoritesCommandType = {
  addressId: Scalars['String']['input'];
};

export type RemoveQuoteItemCommandType = {
  lineItemId: Scalars['String']['input'];
  quoteId: Scalars['String']['input'];
};

export type SalesRepStatisticsPeriodInput = {
  /** Inclusive lower bound of the period (null = no lower bound). */
  from?: InputMaybe<Scalars['DateTime']['input']>;
  /** Inclusive upper bound of the period (null = no upper bound). */
  to?: InputMaybe<Scalars['DateTime']['input']>;
};

export type SendPasswordResetEmailCommandType = {
  cultureName?: InputMaybe<Scalars['String']['input']>;
  loginOrEmail: Scalars['String']['input'];
  storeId?: InputMaybe<Scalars['String']['input']>;
  urlSuffix?: InputMaybe<Scalars['String']['input']>;
};

export type SubmitQuoteCommandType = {
  comment: Scalars['String']['input'];
  quoteId: Scalars['String']['input'];
};

export type SubmitReturnCommandType = {
  returnId: Scalars['String']['input'];
};

export type UpdateQuoteAddressesCommandType = {
  addresses: Array<InputMaybe<InputQuoteAddressType>>;
  quoteId: Scalars['String']['input'];
};

export type UpdateQuoteAttachmentsCommandType = {
  quoteId: Scalars['String']['input'];
  urls: Array<InputMaybe<Scalars['String']['input']>>;
};

export type UpdateQuoteDynamicPropertiesCommandType = {
  /** Dynamic properties */
  dynamicProperties: Array<InputMaybe<InputDynamicPropertyValueType>>;
  quoteId: Scalars['String']['input'];
};

export type UpdateReturnCommandType = {
  customerComment?: InputMaybe<Scalars['String']['input']>;
  customerReference?: InputMaybe<Scalars['String']['input']>;
  items?: InputMaybe<Array<InputReturnItemType>>;
  returnId: Scalars['String']['input'];
};

export enum WishlistAccessType {
  /** Readonly access */
  Read = 'Read',
  /** Write access */
  Write = 'Write'
}

export enum WishlistScopeType {
  /** Anyone (anonymous) scope */
  AnyoneAnonymous = 'AnyoneAnonymous',
  /** Anyone (authorized) scope */
  AnyoneAuthorized = 'AnyoneAuthorized',
  /** Customer scope (shared by a Sales Rep with specific customer organizations) */
  Customer = 'Customer',
  /** Organization scope */
  Organization = 'Organization',
  /** Private scope */
  Private = 'Private',
  /** User scope */
  User = 'User'
}

export type StorefrontMoneyFragment = { amount: number, formattedAmount: string, formattedAmountWithoutCurrency: string };

export type CreateConfiguredLineItemMutationVariables = Exact<{
  command: InputCreateConfiguredLineItemCommand;
}>;


export type CreateConfiguredLineItemMutation = { createConfiguredLineItem?: { listPrice?: { amount: number, formattedAmount: string, formattedAmountWithoutCurrency: string }, salePrice?: { amount: number, formattedAmount: string, formattedAmountWithoutCurrency: string } } };

export type PushHistoricalEventMutationVariables = Exact<{
  command: InputPushHistoricalEventType;
}>;


export type PushHistoricalEventMutation = { pushHistoricalEvent?: boolean };

export const StorefrontMoneyFragmentDoc = {"kind":"Document","definitions":[{"kind":"FragmentDefinition","name":{"kind":"Name","value":"storefrontMoney"},"typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"MoneyType"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"amount"}},{"kind":"Field","name":{"kind":"Name","value":"formattedAmount"}},{"kind":"Field","name":{"kind":"Name","value":"formattedAmountWithoutCurrency"}}]}}]} as unknown as DocumentNode<StorefrontMoneyFragment, unknown>;
export const CreateConfiguredLineItemDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateConfiguredLineItem"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"command"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"InputCreateConfiguredLineItemCommand"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createConfiguredLineItem"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"command"},"value":{"kind":"Variable","name":{"kind":"Name","value":"command"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"listPrice"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"FragmentSpread","name":{"kind":"Name","value":"storefrontMoney"}}]}},{"kind":"Field","name":{"kind":"Name","value":"salePrice"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"FragmentSpread","name":{"kind":"Name","value":"storefrontMoney"}}]}}]}}]}},{"kind":"FragmentDefinition","name":{"kind":"Name","value":"storefrontMoney"},"typeCondition":{"kind":"NamedType","name":{"kind":"Name","value":"MoneyType"}},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"amount"}},{"kind":"Field","name":{"kind":"Name","value":"formattedAmount"}},{"kind":"Field","name":{"kind":"Name","value":"formattedAmountWithoutCurrency"}}]}}]} as unknown as DocumentNode<CreateConfiguredLineItemMutation, CreateConfiguredLineItemMutationVariables>;
export const PushHistoricalEventDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"PushHistoricalEvent"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"command"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"InputPushHistoricalEventType"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"pushHistoricalEvent"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"command"},"value":{"kind":"Variable","name":{"kind":"Name","value":"command"}}}]}]}}]} as unknown as DocumentNode<PushHistoricalEventMutation, PushHistoricalEventMutationVariables>;
export const OperationNames = {
  Mutation: {
    CreateConfiguredLineItem: 'CreateConfiguredLineItem',
    PushHistoricalEvent: 'PushHistoricalEvent'
  },
  Fragment: {
    storefrontMoney: 'storefrontMoney'
  }
}